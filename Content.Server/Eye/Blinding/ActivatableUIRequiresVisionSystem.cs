using Content.Shared.Eye.Blinding;
using Content.Shared.UserInterface;
using Content.Server.Popups;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Robust.Shared.Player;
using Robust.Server.GameObjects;
using Robust.Shared.Collections;

namespace Content.Server.Eye.Blinding;

public sealed class ActivatableUIRequiresVisionSystem : EntitySystem
{
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

        if (TryComp<BlindableComponent>(args.User, out var blindable) && blindable.IsBlind)
        {
            _popupSystem.PopupCursor(Loc.GetString("blindness-fail-attempt"), args.User, Shared.Popups.PopupType.MediumCaution);
            args.Cancel();
        }
    }

    private void OnBlindnessChanged(EntityUid uid, BlindableComponent component, ref BlindnessChangedEvent args)
    {
        if (!args.Blind)
            return;

        var toClose = new ValueList<(EntityUid Entity, Enum Key)>();

        foreach (var bui in _userInterfaceSystem.GetActorUis(uid))
        {
            if (HasComp<ActivatableUIRequiresVisionComponent>(bui.Entity))
            {
                toClose.Add(bui);
            }
        }

        foreach (var bui in toClose)
        {
            _userInterfaceSystem.CloseUi(bui.Entity, bui.Key, uid);
        }
    }
}
