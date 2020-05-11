// Only unused on .NET Core due to KeyValuePair.Deconstruct
// ReSharper disable once RedundantUsingDirective

using System;
using Content.Server.GameObjects.Components.Chemistry;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Audio;
using Content.Shared.Chemistry;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components;
using Content.Shared.GameObjects.Components.Interactable;
using Content.Shared.Maps;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Interactable
{
    [RegisterComponent]
    public class ToolComponent : SharedToolComponent, IExamine, IAfterAttack, IUse, IAttack
    {
#pragma warning disable 649
        [Dependency] private IEntitySystemManager _entitySystemManager;
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager;
        [Dependency] private readonly IMapManager _mapManager;
        [Dependency] private readonly IPrototypeManager _prototypeManager;
        [Dependency] private readonly IRobustRandom _robustRandom;
#pragma warning restore 649

        private AudioSystem _audioSystem;
        private InteractionSystem _interactionSystem;
        private SpriteComponent _spriteComponent;

        protected Tool _behavior = Tool.Wrench;
        private string _useSound;
        private string _useSoundCollection;
        private float _speedModifier = 1;

        [ViewVariables]
        public override Tool Behavior
        {
            get => _behavior;
            set
            {
                _behavior = value;
                Dirty();
            }
        }

        /// <summary>
        ///     For tool interactions that have a delay before action this will modify the rate, time to wait is divided by this value
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float SpeedModifier
        {
            get => _speedModifier;
            set => _speedModifier = value;
        }

        public string UseSound
        {
            get => _useSound;
            set => _useSound = value;
        }

        public string UseSoundCollection
        {
            get => _useSoundCollection;
            set => _useSoundCollection = value;
        }

        public override void Initialize()
        {
            base.Initialize();

            _audioSystem = _entitySystemManager.GetEntitySystem<AudioSystem>();
            _interactionSystem = _entitySystemManager.GetEntitySystem<InteractionSystem>();
            Owner.TryGetComponent(out _spriteComponent);
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            if (serializer.Reading)
            {
                try
                {
                    _behavior = (Tool)serializer.ReadStringEnumKey("behavior");
                }
                catch
                {
                    // ignored
                }
            }
            serializer.DataField(ref _speedModifier, "speed", 1);
            serializer.DataField(ref _useSound, "useSound", string.Empty);
            serializer.DataField(ref _useSoundCollection, "useSoundCollection", string.Empty);
        }

        /// <summary>
        ///     Status modifier which determines whether or not we can act as a tool at this time
        /// </summary>
        public virtual bool CanUse()
        {
            return true;
        }

        protected void PlaySoundCollection(string name, float volume=-5f)
        {
            var soundCollection = _prototypeManager.Index<SoundCollectionPrototype>(name);
            var file = _robustRandom.Pick(soundCollection.PickFiles);
            _entitySystemManager.GetEntitySystem<AudioSystem>()
                .Play(file, Owner, AudioParams.Default.WithVolume(volume));
        }

        public void PlayUseSound()
        {
            if(string.IsNullOrEmpty(UseSoundCollection))
                _audioSystem.Play(UseSound, Owner);
            else
                PlaySoundCollection(UseSoundCollection, 0f);
        }

        public override ComponentState GetComponentState()
        {
            return new ToolComponentState(Behavior);
        }

        public void AfterAttack(AfterAttackEventArgs eventArgs)
        {
            if (Behavior != Tool.Crowbar)
                return;

            var mapGrid = _mapManager.GetGrid(eventArgs.ClickLocation.GridID);
            var tile = mapGrid.GetTileRef(eventArgs.ClickLocation);

            var coordinates = mapGrid.GridTileToLocal(tile.GridIndices);

            if (!_interactionSystem.InRangeUnobstructed(eventArgs.User.Transform.MapPosition, coordinates.ToMapPos(_mapManager), ignoredEnt:eventArgs.User))
                return;

            var tileDef = (ContentTileDefinition)_tileDefinitionManager[tile.Tile.TypeId];

            if (!tileDef.CanCrowbar) return;

            var underplating = _tileDefinitionManager["underplating"];
            mapGrid.SetTile(eventArgs.ClickLocation, new Tile(underplating.TileId));
            PlayUseSound();

            //Actually spawn the relevant tile item at the right position and give it some offset to the corner.
            var tileItem = Owner.EntityManager.SpawnEntity(tileDef.ItemDropPrototypeName, coordinates);
            tileItem.Transform.WorldPosition += (0.2f, 0.2f);
        }

        public virtual bool UseEntity(UseEntityEventArgs eventArgs)
        {
            return false;
        }

        public virtual void Examine(FormattedMessage message)
        {
        }

        public virtual void Attack(AttackEventArgs eventArgs)
        {
        }
    }
}
