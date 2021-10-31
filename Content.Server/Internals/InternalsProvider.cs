using Content.Server.Atmos.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Content.Shared.Hands.Components;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.IoC;
using Content.Shared.Interaction.Helpers;
using Content.Shared.ActionBlocker;
using Content.Server.Stack;
using Content.Server.Tools;
using Content.Server.Tools.Components;
using Content.Shared.Tools;
using Content.Shared.Tools.Components;
using System.Threading.Tasks;
using Content.Shared.Item;
using Robust.Shared.Map;
using Robust.Shared.ViewVariables;
using Robust.Shared.Localization;
using Content.Server.Body.Respiratory;

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
            SubscribeLocalEvent<InternalsProviderComponent, ToggleInternalsEvent>(OnToggleInternals);
        }

        public void OnShutdown(EntityUid uid, InternalsProviderComponent component, ComponentShutdown args)
        {
            DisconnectFromInternals(uid, component);
        }

        public void OnExamined(EntityUid uid, InternalsProviderComponent component, ExaminedEvent args)
        {
            if (component.Connected) {
                if (!args.IsInDetailsRange)
                    return;
                args.PushText("");
                args.PushMarkup(Loc.GetString("comp-gas-tank-connected", ("connectedTo", GetConnectedTo(component)?.Name ?? "ERROR-NULL-ENTITY")));
            }
        }

        public void OnSuitExamined(EntityUid uid, InternalsSuitComponent suit, InternalsProviderComponent component, ExaminedEvent args)
        {
            if (component.Connected) {
                args.PushText("");
                args.PushMarkup(Loc.GetString(
                    "comp-gas-tank-inside-connected",
                    ("name", component.Owner.Name),
                    ("connectedTo", GetConnectedTo(component)?.Name ?? "ERROR-NULL-ENTITY")
                ));
            }
        }

        public void OnDropped(EntityUid uid, InternalsProviderComponent component, DroppedEvent args)
        {
            DisconnectFromInternals(uid, component);
        }

        public void OnToggleInternals(EntityUid uid, InternalsProviderComponent component, ToggleInternalsEvent args)
        {
            ToggleInternals(uid, component, args);
        }

        public bool ToggleInternals(EntityUid uid, InternalsProviderComponent component, ToggleInternalsEvent args) {

            var user = GetInternalsComponent(component)?.Owner;

            if (user == null || !Get<ActionBlockerSystem>().CanUse(user))
                return false;

            if (args.ForcedState == true || (args.ForcedState == null && !component.Connected))
            {
                ConnectToInternals(uid, component);
                return component.Connected;
            }
            else if (args.ForcedState == false || (args.ForcedState == null && component.Connected))
            {
                DisconnectFromInternals(uid, component);
                return !component.Connected;
            }
            return false;
        }

        public void ConnectToInternals(EntityUid uid, InternalsProviderComponent component)
        {
            if (component.Connected || !IsFunctional(component)) return;
            var internals = GetInternalsComponent(component);

            if (internals == null) return;
            component.Connected = internals.TryConnectTank(component.Owner);
        }

        public void DisconnectFromInternals(EntityUid uid, InternalsProviderComponent component)
        {
            if (!component.Connected) return;
            component.Connected = false;
            GetInternalsComponent(component)?.DisconnectTank();
        }

        private IEntity? GetConnectedTo(InternalsProviderComponent component)
        {
            var internals = GetInternalsComponent(component);

            return internals?.BreathToolEntity;
        }

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

        private bool IsFunctional(InternalsProviderComponent component)
        {
            return GetInternalsComponent(component) != null;
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
        public bool Connected { get; set; }
    }
}
