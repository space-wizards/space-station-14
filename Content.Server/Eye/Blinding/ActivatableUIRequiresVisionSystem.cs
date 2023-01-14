using Content.Shared.Eye.Blinding;
using Content.Server.UserInterface;
using Content.Server.Popups;
using Robust.Shared.Player;
using Robust.Server.GameObjects;

namespace Content.Server.Eye.Blinding
{
    public sealed class ActivatableUIRequiresVisionSystem : EntitySystem
    {
        [Dependency] private readonly ActivatableUISystem _activatableUISystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ActivatableUIRequiresVisionComponent, ActivatableUIOpenAttemptEvent>(OnOpenAttempt);
            SubscribeLocalEvent<BlindableComponent, BlindnessChangedEvent>(OnBlindnessChanged);
        }

        private void OnOpenAttempt(EntityUid uid, ActivatableUIRequiresVisionComponent component, ActivatableUIOpenAttemptEvent args)
        {
            if (args.Cancelled)
                return;

            if (TryComp<BlindableComponent>(args.User, out var blindable) && blindable.Sources > 0)
            {
                _popupSystem.PopupCursor(Loc.GetString("blindness-fail-attempt"), args.User, Shared.Popups.PopupType.MediumCaution);
                args.Cancel();
            }
        }

        private void OnBlindnessChanged(EntityUid uid, BlindableComponent component, BlindnessChangedEvent args)
        {
            if (!args.Blind)
                return;

            if (!TryComp<ActorComponent>(uid, out var actor))
                return;

            var uiList = _userInterfaceSystem.GetAllUIsForSession(actor.PlayerSession);
            if (uiList == null)
                return;

            Queue<BoundUserInterface> closeList = new(); // foreach collection modified moment

            foreach (var ui in uiList)
            {
                if (HasComp<ActivatableUIRequiresVisionComponent>(ui.Owner))
                    closeList.Enqueue(ui);
            }

            foreach (var ui in closeList)
            {
                _userInterfaceSystem.CloseUi(ui, actor.PlayerSession);
            }
        }
    }
}
