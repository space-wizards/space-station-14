using System.Diagnostics.CodeAnalysis;
using Content.Client.Popups;
using Content.Shared.Construction;
using Content.Shared.Construction.Prototypes;
using Content.Shared.Examine;
using Content.Shared.Input;
using Content.Shared.Interaction;
using Content.Shared.Wall;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.Construction
{
    /// <summary>
    /// The client-side implementation of the construction system, which is used for constructing entities in game.
    /// </summary>
    [UsedImplicitly]
    public sealed class ConstructionSystem : SharedConstructionSystem
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;

        private readonly Dictionary<int, EntityUid> _ghosts = new();
        private readonly Dictionary<string, ConstructionGuide> _guideCache = new();

        public bool CraftingEnabled { get; private set; }

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();

            UpdatesOutsidePrediction = true;
            SubscribeLocalEvent<LocalPlayerAttachedEvent>(HandlePlayerAttached);
            SubscribeNetworkEvent<AckStructureConstructionMessage>(HandleAckStructure);
            SubscribeNetworkEvent<ResponseConstructionGuide>(OnConstructionGuideReceived);

            CommandBinds.Builder
                .Bind(ContentKeyFunctions.OpenCraftingMenu,
                    new PointerInputCmdHandler(HandleOpenCraftingMenu, outsidePrediction:true))
                .Bind(EngineKeyFunctions.Use,
                    new PointerInputCmdHandler(HandleUse, outsidePrediction: true))
                .Bind(ContentKeyFunctions.EditorFlipObject,
                    new PointerInputCmdHandler(HandleFlip, outsidePrediction:true))
                .Register<ConstructionSystem>();

            SubscribeLocalEvent<ConstructionGhostComponent, ExaminedEvent>(HandleConstructionGhostExamined);
        }

        private void OnConstructionGuideReceived(ResponseConstructionGuide ev)
        {
            _guideCache[ev.ConstructionId] = ev.Guide;
            ConstructionGuideAvailable?.Invoke(this, ev.ConstructionId);
        }

        /// <inheritdoc />
        public override void Shutdown()
        {
            base.Shutdown();

            CommandBinds.Unregister<ConstructionSystem>();
        }

        public ConstructionGuide? GetGuide(ConstructionPrototype prototype)
        {
            if (_guideCache.TryGetValue(prototype.ID, out var guide))
                return guide;

            RaiseNetworkEvent(new RequestConstructionGuide(prototype.ID));
            return null;
        }

        private void HandleConstructionGhostExamined(EntityUid uid, ConstructionGhostComponent component, ExaminedEvent args)
        {
            if (component.Prototype == null) return;

            args.PushMarkup(Loc.GetString(
                "construction-ghost-examine-message",
                ("name", component.Prototype.Name)));

            if (!_prototypeManager.TryIndex(component.Prototype.Graph, out ConstructionGraphPrototype? graph))
                return;

            var startNode = graph.Nodes[component.Prototype.StartNode];

            if (!graph.TryPath(component.Prototype.StartNode, component.Prototype.TargetNode, out var path) ||
                !startNode.TryGetEdge(path[0].Name, out var edge))
            {
                return;
            }

            edge.Steps[0].DoExamine(args);
        }

        public event EventHandler<CraftingAvailabilityChangedArgs>? CraftingAvailabilityChanged;
        public event EventHandler<string>? ConstructionGuideAvailable;
        public event EventHandler? ToggleCraftingWindow;
        public event EventHandler? FlipConstructionPrototype;

        private void HandleAckStructure(AckStructureConstructionMessage msg)
        {
            // We get sent a NetEntity but it actually corresponds to our local Entity.
            ClearGhost(msg.GhostId);
        }

        private void HandlePlayerAttached(LocalPlayerAttachedEvent msg)
        {
            var available = IsCraftingAvailable(msg.Entity);
            UpdateCraftingAvailability(available);
        }

        private bool HandleOpenCraftingMenu(in PointerInputCmdHandler.PointerInputCmdArgs args)
        {
            if (args.State == BoundKeyState.Down)
                ToggleCraftingWindow?.Invoke(this, EventArgs.Empty);
            return true;
        }

        private bool HandleFlip(in PointerInputCmdHandler.PointerInputCmdArgs args)
        {
            if (args.State == BoundKeyState.Down)
                FlipConstructionPrototype?.Invoke(this, EventArgs.Empty);
            return true;
        }

        private void UpdateCraftingAvailability(bool available)
        {
            if (CraftingEnabled == available)
                return;

            CraftingAvailabilityChanged?.Invoke(this, new CraftingAvailabilityChangedArgs(available));
            CraftingEnabled = available;
        }

        private static bool IsCraftingAvailable(EntityUid? entity)
        {
            if (entity == default)
                return false;

            // TODO: Decide if entity can craft, using capabilities or something
            return true;
        }

        private bool HandleUse(in PointerInputCmdHandler.PointerInputCmdArgs args)
        {
            if (!args.EntityUid.IsValid() || !IsClientSide(args.EntityUid))
                return false;

            if (!HasComp<ConstructionGhostComponent>(args.EntityUid))
                return false;

            TryStartConstruction(args.EntityUid);
            return true;
        }

        /// <summary>
        /// Creates a construction ghost at the given location.
        /// </summary>
        public void SpawnGhost(ConstructionPrototype prototype, EntityCoordinates loc, Direction dir)
            => TrySpawnGhost(prototype, loc, dir, out _);

        /// <summary>
        /// Creates a construction ghost at the given location.
        /// </summary>
        public bool TrySpawnGhost(
            ConstructionPrototype prototype,
            EntityCoordinates loc,
            Direction dir,
            [NotNullWhen(true)] out EntityUid? ghost)
        {
            ghost = null;
            if (_playerManager.LocalPlayer?.ControlledEntity is not { } user ||
                !user.IsValid())
            {
                return false;
            }

            if (GhostPresent(loc))
                return false;

            // This InRangeUnobstructed should probably be replaced with "is there something blocking us in that tile?"
            var predicate = GetPredicate(prototype.CanBuildInImpassable, loc.ToMap(EntityManager));
            if (!_interactionSystem.InRangeUnobstructed(user, loc, 20f, predicate: predicate))
                return false;

            if (!CheckConstructionConditions(prototype, loc, dir, user, showPopup: true))
                return false;

            ghost = EntityManager.SpawnEntity("constructionghost", loc);
            var comp = EntityManager.GetComponent<ConstructionGhostComponent>(ghost.Value);
            comp.Prototype = prototype;
            EntityManager.GetComponent<TransformComponent>(ghost.Value).LocalRotation = dir.ToAngle();
            _ghosts.Add(ghost.GetHashCode(), ghost.Value);
            var sprite = EntityManager.GetComponent<SpriteComponent>(ghost.Value);
            sprite.Color = new Color(48, 255, 48, 128);

            for (int i = 0; i < prototype.Layers.Count; i++)
            {
                sprite.AddBlankLayer(i); // There is no way to actually check if this already exists, so we blindly insert a new one
                sprite.LayerSetSprite(i, prototype.Layers[i]);
                sprite.LayerSetShader(i, "unshaded");
                sprite.LayerSetVisible(i, true);
            }

            if (prototype.CanBuildInImpassable)
                EnsureComp<WallMountComponent>(ghost.Value).Arc = new(Math.Tau);

            return true;
        }

        private bool CheckConstructionConditions(ConstructionPrototype prototype, EntityCoordinates loc, Direction dir,
            EntityUid user, bool showPopup = false)
        {
            foreach (var condition in prototype.Conditions)
            {
                if (!condition.Condition(user, loc, dir))
                {
                    if (showPopup)
                    {
                        var message = condition.GenerateGuideEntry()?.Localization;
                        if (message != null)
                        {
                            // Show the reason to the user:
                            _popupSystem.PopupCoordinates(Loc.GetString(message), loc);
                        }
                    }

                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks if any construction ghosts are present at the given position
        /// </summary>
        private bool GhostPresent(EntityCoordinates loc)
        {
            foreach (var ghost in _ghosts)
            {
                if (EntityManager.GetComponent<TransformComponent>(ghost.Value).Coordinates.Equals(loc))
                    return true;
            }

            return false;
        }

        public void TryStartConstruction(EntityUid ghostId, ConstructionGhostComponent? ghostComp = null)
        {
            if (!Resolve(ghostId, ref ghostComp))
                return;

            if (ghostComp.Prototype == null)
            {
                throw new ArgumentException($"Can't start construction for a ghost with no prototype. Ghost id: {ghostId}");
            }

            var transform = EntityManager.GetComponent<TransformComponent>(ghostId);
            var msg = new TryStartStructureConstructionMessage(GetNetCoordinates(transform.Coordinates), ghostComp.Prototype.ID, transform.LocalRotation, ghostId.GetHashCode());
            RaiseNetworkEvent(msg);
        }

        /// <summary>
        /// Starts constructing an item underneath the attached entity.
        /// </summary>
        public void TryStartItemConstruction(string prototypeName)
        {
            RaiseNetworkEvent(new TryStartItemConstructionMessage(prototypeName));
        }

        /// <summary>
        /// Removes a construction ghost entity with the given ID.
        /// </summary>
        public void ClearGhost(int ghostId)
        {
            if (!_ghosts.TryGetValue(ghostId, out var ghost))
                return;

            EntityManager.QueueDeleteEntity(ghost);
            _ghosts.Remove(ghostId);
        }

        /// <summary>
        /// Removes all construction ghosts.
        /// </summary>
        public void ClearAllGhosts()
        {
            foreach (var ghost in _ghosts.Values)
            {
                EntityManager.QueueDeleteEntity(ghost);
            }

            _ghosts.Clear();
        }
    }

    public sealed class CraftingAvailabilityChangedArgs : EventArgs
    {
        public bool Available { get; }

        public CraftingAvailabilityChangedArgs(bool available)
        {
            Available = available;
        }
    }
}
