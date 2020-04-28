// Only unused on .NET Core due to KeyValuePair.Deconstruct
// ReSharper disable once RedundantUsingDirective

using Content.Server.GameObjects.Components.Chemistry;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Maps;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Interactable
{
    public enum Tool : byte
    {
        Wrench,
        Crowbar,
        Screwdriver,
        Wirecutters,
        Welder,
        Multitool,
    }

    public abstract class ToolComponent : Component, IExamine, IAfterAttack
    {
#pragma warning disable 649
        [Dependency] private IEntitySystemManager _entitySystemManager;
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager;
        [Dependency] private readonly IMapManager _mapManager;
#pragma warning restore 649

        private AudioSystem _audioSystem;
        private InteractionSystem _interactionSystem;
        private SolutionComponent

        private Tool _behavior;
        private bool _activated = false;

        public override string Name => "Tool";

        public Tool Behavior
        {
            get => _behavior;
            set => _behavior = value;
        }

        public override void Initialize()
        {
            base.Initialize();

            _audioSystem = _entitySystemManager.GetEntitySystem<AudioSystem>();
            _interactionSystem = _entitySystemManager.GetEntitySystem<InteractionSystem>();
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _behavior, "behavior", Tool.Wrench);
            serializer.DataField(ref _speedModifier, "Speed", 1);
        }

        public void Examine(FormattedMessage message)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// For tool interactions that have a delay before action this will modify the rate, time to wait is divided by this value
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float SpeedModifier
        {
            get => _speedModifier;
            set => _speedModifier = value;
        }
        private float _speedModifier = 1;

        /// <summary>
        /// Status modifier which determines whether or not we can act as a tool at this time
        /// </summary>
        /// <returns></returns>
        public virtual bool CanUse()
        {
            if (_behavior != Tool.Welder)
                return true;
        }

        /// <summary>
        /// Default Cost of using the welder fuel for an action
        /// </summary>
        public const float DefaultFuelCost = 5;

        /// <summary>
        /// Rate at which we expunge fuel from ourselves when activated
        /// </summary>
        public const float FuelLossRate = 0.2f;

        /// <summary>
        /// Status of welder, whether it is ignited
        /// </summary>
        [ViewVariables]
        public bool Activated
        {
            get => _activated;
            private set
            {
                _activated = value;
                Dirty();
            }
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
            _audioSystem.Play("/Audio/items/crowbar.ogg", Owner);

            //Actually spawn the relevant tile item at the right position and give it some offset to the corner.
            var tileItem = Owner.EntityManager.SpawnEntity(tileDef.ItemDropPrototypeName, coordinates);
            tileItem.Transform.WorldPosition += (0.2f, 0.2f);
        }
    }
}
