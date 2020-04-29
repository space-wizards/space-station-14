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
        /// <summary>
        /// Default Cost of using the welder fuel for an action
        /// </summary>
        public const float DefaultFuelCost = 10;

        /// <summary>
        /// Rate at which we expunge fuel from ourselves when activated
        /// </summary>
        public const float FuelLossRate = 0.5f;

#pragma warning disable 649
        [Dependency] private IEntitySystemManager _entitySystemManager;
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager;
        [Dependency] private readonly IMapManager _mapManager;
        [Dependency] private readonly IPrototypeManager _prototypeManager;
        [Dependency] private readonly IRobustRandom _robustRandom;
#pragma warning restore 649

        private AudioSystem _audioSystem;
        private InteractionSystem _interactionSystem;
        private ToolSystem _toolSystem;

        private SolutionComponent _solutionComponent;
        private SpriteComponent _spriteComponent;

        private Tool _behavior = Tool.Wrench;
        private float _speedModifier = 1;
        private bool _welderLit = false;
        private string _useSound;
        private string _useSoundCollection;

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

        [ViewVariables]
        public float Fuel => _solutionComponent?.Solution.GetReagentQuantity("chem.WeldingFuel").Float() ?? 0f;

        [ViewVariables]
        public float FuelCapacity => _solutionComponent?.MaxVolume.Float() ?? 0f;

        /// <summary>
        ///     For tool interactions that have a delay before action this will modify the rate, time to wait is divided by this value
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float SpeedModifier
        {
            get => _speedModifier;
            set => _speedModifier = value;
        }

        /// <summary>
        /// Status of welder, whether it is ignited
        /// </summary>
        [ViewVariables]
        public bool WelderLit
        {
            get => _welderLit;
            private set
            {
                _welderLit = value;
                Dirty();
            }
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
            _toolSystem = _entitySystemManager.GetEntitySystem<ToolSystem>();

            Owner.TryGetComponent(out _solutionComponent);
            Owner.TryGetComponent(out _spriteComponent);
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            if(serializer.Reading)
                _behavior = (Tool)serializer.ReadStringEnumKey("behavior");
            serializer.DataField(ref _speedModifier, "speed", 1);
            serializer.DataField(ref _useSound, "useSound", string.Empty);
            serializer.DataField(ref _useSoundCollection, "useSoundCollection", string.Empty);
        }

        /// <summary>
        ///     Status modifier which determines whether or not we can act as a tool at this time
        /// </summary>
        public bool CanUse()
        {
            return _behavior != Tool.Welder || CanWeld(DefaultFuelCost);
        }

        public bool TryWeld(float value)
        {
            if (!WelderLit || !CanWeld(value) || _solutionComponent == null)
            {
                return false;
            }

            return _solutionComponent.TryRemoveReagent("chem.WeldingFuel", ReagentUnit.New(value));
        }

        public bool CanWeld(float value)
        {
            return Fuel > value || Behavior != Tool.Welder;
        }

        public bool CanLitWelder()
        {
            return Fuel > 0 || Behavior != Tool.Welder;
        }

        /// <summary>
        /// Deactivates welding tool if active, activates welding tool if possible
        /// </summary>
        /// <returns></returns>
        public bool ToggleWelderStatus()
        {
            if (WelderLit)
            {
                WelderLit = false;
                // Layer 1 is the flame.
                _spriteComponent.LayerSetVisible(1, false);
                PlaySoundCollection("WelderOff", -5);
                _toolSystem.Unsubscribe(this);
                return true;
            }

            if (!CanLitWelder()) return false;

            WelderLit = true;
            _spriteComponent.LayerSetVisible(1, true);
            PlaySoundCollection("WelderOn", -5);
            _toolSystem.Subscribe(this);
            return true;
        }

        public void OnUpdate(float frameTime)
        {
            if (Behavior != Tool.Welder || !WelderLit)
            {
                return;
            }

            _solutionComponent.TryRemoveReagent("chem.WeldingFuel", ReagentUnit.New(FuelLossRate * frameTime));

            if (Fuel == 0)
            {
                ToggleWelderStatus();
            }

            Dirty();
        }

        private void PlaySoundCollection(string name, float volume=-5f)
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
            return Behavior == Tool.Welder ? new ToolComponentState(FuelCapacity, Fuel, WelderLit) : new ToolComponentState(Behavior);
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

        public bool UseEntity(UseEntityEventArgs eventArgs)
        {
            Logger.Info(Behavior.ToString());

            switch (Behavior)
            {
                case Tool.Welder:
                    return ToggleWelderStatus();
            }

            return false;
        }

        public void Examine(FormattedMessage message)
        {
            switch (Behavior)
            {
                case Tool.Welder:
                    if (WelderLit)
                    {
                        message.AddMarkup(Loc.GetString("[color=orange]Lit[/color]\n"));
                    }
                    else
                    {
                        message.AddText(Loc.GetString("Not lit\n"));
                    }

                    message.AddMarkup(Loc.GetString("Fuel: [color={0}]{1}/{2}[/color].",
                        Fuel < FuelCapacity / 4f ? "darkorange" : "orange", Math.Round(Fuel), FuelCapacity));
                    break;
            }
        }

        public void Attack(AttackEventArgs eventArgs)
        {

        }
    }
}
