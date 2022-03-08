using Content.Shared.Popups;
using Content.Server.Power.Components;
using Content.Server.UserInterface;
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
            if (EntityManager.TryGetComponent<ApcPowerReceiverComponent>(uid, out var power) && !power.Powered)
            {
                args.User.PopupMessageCursor(Loc.GetString("base-computer-ui-component-not-powered"));
                args.Cancel();
            }
        }

        private void OnPowerChanged(EntityUid uid, ActivatableUIRequiresPowerComponent component, PowerChangedEvent args)
        {
            if (!args.Powered)
                _activatableUISystem.CloseAll(uid);
        }
    }
}
