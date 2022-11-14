using Content.Shared.Popups;
using Content.Server.Power.Components;
using Content.Server.UserInterface;
using Content.Server.Wires;
using JetBrains.Annotations;

namespace Content.Server.Power.EntitySystems
{
    [UsedImplicitly]
    internal sealed class ActivatableUIRequiresPowerSystem : EntitySystem
    {
        [Dependency] private readonly ActivatableUISystem _activatableUISystem = default!;
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ActivatableUIRequiresPowerComponent, ActivatableUIOpenAttemptEvent>(OnActivate);
            SubscribeLocalEvent<ActivatableUIRequiresPowerComponent, PowerChangedEvent>(OnPowerChanged);
        }

        private void OnActivate(EntityUid uid, ActivatableUIRequiresPowerComponent component, ActivatableUIOpenAttemptEvent args)
        {
            if (args.Cancelled) return;
            if (this.IsPowered(uid, EntityManager)) return;
            if (TryComp<WiresComponent>(uid, out var wires) && wires.IsPanelOpen)
                return;
            args.User.PopupMessageCursor(Loc.GetString("base-computer-ui-component-not-powered", ("machine", uid)));
            args.Cancel();
        }

        private void OnPowerChanged(EntityUid uid, ActivatableUIRequiresPowerComponent component, ref PowerChangedEvent args)
        {
            if (!args.Powered)
                _activatableUISystem.CloseAll(uid);
        }
    }
}
