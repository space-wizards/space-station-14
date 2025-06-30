using Content.Shared.Examine;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared.AirTank;

public sealed class AirTankLooksLikeSystem : EntitySystem
{
    [Dependency] private readonly ILocalizationManager _localization = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly INetManager _netMan = default!;

    public readonly LocId PopupText = "air-tank-looks-like";
    public readonly LocId DontText = "air-tank-looks-like-dont";
    public readonly LocId ConfirmText = "air-tank-looks-like-confirm";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AirTankLooksLikeComponent, ExaminedEvent>(OnExamine);
    }

    public void CreateShouldntBreathePopup(EntityUid user, EntityUid tank)
    {
        if (!TryComp(tank, out AirTankLooksLikeComponent? airTankLooksLike))
            return;

        string PopupTextLoc = _localization.GetString(PopupText, ("state", airTankLooksLike.Contains));
        string ConfirmTextLoc = _localization.GetString(ConfirmText);

        _popupSystem.PopupPredicted(PopupTextLoc + "\n" + ConfirmTextLoc, null, user, user, PopupType.MediumCaution);
    }

    public void OnExamine(Entity<AirTankLooksLikeComponent> uid, ref ExaminedEvent args)
    {
        if (!TryComp(args.Examiner, out AirTankShouldBreatheComponent? shouldBreathe))
            return;

        if (!CanBreathe(shouldBreathe, uid.Comp))
            args.PushMarkup(Loc.GetString(DontText));
    }

    public bool CanBreathe(AirTankShouldBreatheComponent user, AirTankLooksLikeComponent tank)
    {
        return user.TankTypes.Contains(tank.Contains);
    }

    public bool CheckShouldBreathe(EntityUid user, EntityUid tank)
    {
        if (!TryComp(user, out AirTankShouldBreatheComponent? shouldBreathe) ||
            !TryComp(tank, out AirTankLooksLikeComponent? looksLike))
            return true;

        if (CanBreathe(shouldBreathe, looksLike))
        {
            shouldBreathe.LastAttempted = null;
            return true;
        }

        if (shouldBreathe.LastAttempted == tank)
        {
            return true;
        }

        shouldBreathe.LastAttempted = tank;

        return false;
    }
}
