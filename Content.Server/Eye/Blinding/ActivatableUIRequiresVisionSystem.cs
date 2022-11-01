using Content.Shared.Eye.Blinding;
using Content.Server.UserInterface;
using Content.Server.Popups;
using Robust.Shared.Player;

namespace Content.Server.Eye.Blinding
{
    public sealed class ActivatableUIRequiresVisionSystem : EntitySystem
    {
        [Dependency] private readonly ActivatableUISystem _activatableUISystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ActivatableUIRequiresVisionComponent, ActivatableUIOpenAttemptEvent>(OnOpenAttempt);
        }

        private void OnOpenAttempt(EntityUid uid, ActivatableUIRequiresVisionComponent component, ActivatableUIOpenAttemptEvent args)
        {
            if (args.Cancelled)
                return;

            if (TryComp<BlindableComponent>(args.User, out var blindable) && blindable.Sources > 0)
            {
                _popupSystem.PopupCursor(Loc.GetString("blindness-fail-attempt"), Filter.Entities(args.User), Shared.Popups.PopupType.MediumCaution);
                args.Cancel();
            }
        }

    }
}
