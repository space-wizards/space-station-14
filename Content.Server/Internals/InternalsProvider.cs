using Content.Shared.Examine;
using Content.Shared.Interaction;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Content.Shared.ActionBlocker;
using Robust.Shared.ViewVariables;
using Robust.Shared.Localization;
using Content.Server.Body.Respiratory;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;

namespace Content.Server.Internals
{
    [UsedImplicitly]
    public class InternalsProviderSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<InternalsProviderComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<InternalsProviderComponent, DroppedEvent>(OnDropped);
            SubscribeLocalEvent<InternalsProviderComponent, EntInsertedIntoContainerMessage>(OnContainerInserted);
            SubscribeLocalEvent<InternalsProviderComponent, EntRemovedFromContainerMessage>(OnContainerRemoved);
            SubscribeLocalEvent<InternalsProviderComponent, ToggleInternalsEvent>(OnToggleInternals);
        }

        public void OnShutdown(EntityUid uid, InternalsProviderComponent component, ComponentShutdown args)
            => DisconnectFromInternals(uid, component);

        public void OnExamined(EntityUid uid, InternalsProviderComponent component, ExaminedEvent args)
        {
            if (component.Connected) {
                if (!args.IsInDetailsRange)
                    return;
                args.PushMarkup(Loc.GetString(
                    "comp-gas-tank-connected",
                    ("connectedTo", component.ConnectedTo?.Name ?? "ERROR-NULL-ENTITY")
                ));
            }
        }

        public void OnSuitExamined(EntityUid uid, InternalsSuitComponent suit, InternalsProviderComponent component, ExaminedEvent args)
        {
            if (component.Connected) {
                args.PushMarkup(Loc.GetString(
                    "comp-gas-tank-inside-connected",
                    ("name", component.Owner.Name),
                    ("connectedTo", component.ConnectedTo?.Name ?? "ERROR-NULL-ENTITY")
                ));
            }
        }

        public void OnDropped(EntityUid uid, InternalsProviderComponent component, DroppedEvent args)
            => DisconnectFromInternals(uid, component);

        public void OnContainerInserted(EntityUid uid, InternalsProviderComponent component, EntInsertedIntoContainerMessage args)
            => DisconnectFromInternals(uid, component);

        public void OnContainerRemoved(EntityUid uid, InternalsProviderComponent component, EntRemovedFromContainerMessage args)
            => DisconnectFromInternals(uid, component);

        public void OnToggleInternals(EntityUid uid, InternalsProviderComponent component, ToggleInternalsEvent args)
        {
            if (args.BypassCheck == false)
            {
                var user = GetInternalsComponent(component)?.Owner; // Todo: grab from event
                if (user == null || !Get<ActionBlockerSystem>().CanUse(user))
                    return;
            }

            //Logger.DebugS("internals", $"Trying to set internals @{uid} to \"{args.ForcedState ?? !component.Connected}\" - {(args.ForcedState != null ? "Forced ": "")}{(args.BypassCheck ? "Bypassed!" : "")}");

            var wasConnected = component.Connected;
            if (args.ForcedState == true || (args.ForcedState == null && !component.Connected))
                ConnectToInternals(uid, component);
            else if (args.ForcedState == false || (args.ForcedState == null && component.Connected))
                DisconnectFromInternals(uid, component);

            args.Handled = wasConnected != component.Connected || component.Connected == args.ForcedState;

            if (args.Actions is not null)
                args.Actions.GrantOrUpdate(ItemActionType.ToggleInternals, GetInternalsComponent(component) != null, component.Connected);
            else
            {
                ItemActionsComponent? actions = null;
                if (Resolve(uid, ref actions))
                    actions.GrantOrUpdate(ItemActionType.ToggleInternals, GetInternalsComponent(component) != null, component.Connected);
            }

            if (args.BypassCheck && !args.Handled)
                Logger.ErrorS("internals", $"Internals @{uid} state change failed to change to \"{args.ForcedState}\" despite being forced to bypass a usability check");
        }

        public void ConnectToInternals(EntityUid uid, InternalsProviderComponent component)
        {
            if (component.Connected) return;

            var internals = GetInternalsComponent(component);
            if (internals?.TryConnectTank(component.Owner) ?? false)
                component.Internals = internals;
        }

        public void DisconnectFromInternals(EntityUid uid, InternalsProviderComponent component)
        {
            if (!component.Connected) return;

            //Logger.DebugS("internals", $"Disconnecting internals @{uid} from {component.Internals!.Owner.Name}");

            var internals = component.Internals!;
            component.Internals = null;
            internals.DisconnectTank(true);

            ItemActionsComponent? actions = null;
            if (Resolve(uid, ref actions))
                actions.GrantOrUpdate(ItemActionType.ToggleInternals, GetInternalsComponent(component) != null, component.Connected);
        }

        // It's rather hacky but it only needs to happen once, when trying to turn it on, so it should be alright?
        // Kinda a fair bit of edge cases here
        private InternalsComponent? GetInternalsComponent(InternalsProviderComponent component)
        {
            var owner = component.Owner;
            if (owner.Deleted) return null;

            if (!owner.TryGetContainer(out var container))
                return null;

            if (container.Owner.TryGetComponent(out InternalsComponent? internals))
                return internals!;

            if (container.Owner.TryGetComponent(out InternalsSuitComponent? suit))
                if (suit.Owner.TryGetContainer(out var suitContainer))
                    return suitContainer?.Owner.GetComponentOrNull<InternalsComponent>();

            return null;
        }
    }

    [RegisterComponent]
    public class InternalsProviderComponent : Component
    {
        public override string Name => "InternalsProvider";

        /// <summary>
        ///     Tank is connected to internals.
        /// </summary>
        [ViewVariables]
        public bool Connected { get => Internals is not null; }

        [ViewVariables]
        public IEntity? ConnectedTo { get => Internals?.BreathToolEntity; }

        [ViewVariables]
        public InternalsComponent? Internals { get; set; }
    }
}
