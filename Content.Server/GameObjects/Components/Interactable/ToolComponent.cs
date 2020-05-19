// Only unused on .NET Core due to KeyValuePair.Deconstruct
// ReSharper disable once RedundantUsingDirective

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
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
    public class ToolComponent : SharedToolComponent, IAfterAttack
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

        protected ToolQuality _qualities = ToolQuality.Anchoring;
        private string _useSound;
        private string _useSoundCollection;
        private float _speedModifier = 1;

        [ViewVariables]
        public override ToolQuality Qualities
        {
            get => _qualities;
            set
            {
                _qualities = value;
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

        public void AddQuality(ToolQuality quality)
        {
            _qualities |= quality;
            Dirty();
        }

        public void RemoveQuality(ToolQuality quality)
        {
            _qualities &= ~quality;
            Dirty();
        }

        public bool HasQuality(ToolQuality quality)
        {
            return _qualities.HasFlag(quality);
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
                var qualities = serializer.ReadDataField("qualities", new List<ToolQuality>());
                foreach (var quality in qualities)
                {
                    AddQuality(quality);
                }
            }
            serializer.DataField(ref _speedModifier, "speed", 1);
            serializer.DataField(ref _useSound, "useSound", string.Empty);
            serializer.DataField(ref _useSoundCollection, "useSoundCollection", string.Empty);
        }

        public virtual bool UseTool(IEntity user, IEntity target, ToolQuality toolQualityNeeded)
        {
            PlayUseSound();

            return ActionBlockerSystem.CanInteract(user) && HasQuality(toolQualityNeeded);
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

        public void AfterAttack(AfterAttackEventArgs eventArgs)
        {
            TryPryTile(eventArgs.User, eventArgs.ClickLocation);
        }

        public bool TryPryTile(IEntity user, GridCoordinates clickLocation)
        {
            if (HasQuality(ToolQuality.Prying))
                return false;

            var mapGrid = _mapManager.GetGrid(clickLocation.GridID);
            var tile = mapGrid.GetTileRef(clickLocation);

            var coordinates = mapGrid.GridTileToLocal(tile.GridIndices);

            if (!_interactionSystem.InRangeUnobstructed(user.Transform.MapPosition, coordinates.ToMapPos(_mapManager), ignoredEnt:user))
                return false;

            var tileDef = (ContentTileDefinition)_tileDefinitionManager[tile.Tile.TypeId];

            if (!tileDef.CanCrowbar) return false;

            var underplating = _tileDefinitionManager["underplating"];
            mapGrid.SetTile(clickLocation, new Tile(underplating.TileId));
            PlayUseSound();

            //Actually spawn the relevant tile item at the right position and give it some offset to the corner.
            var tileItem = Owner.EntityManager.SpawnEntity(tileDef.ItemDropPrototypeName, coordinates);
            tileItem.Transform.WorldPosition += (0.2f, 0.2f);
            return true;
        }
    }
}
