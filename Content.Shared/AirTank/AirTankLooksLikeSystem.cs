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

    public readonly LocId ConfirmText = "air-tank-looks-like-confirm";

    public void CreateShouldntBreathePopup(EntityUid user, EntityUid tank)
    {
        if (!TryComp(tank, out AirTankLooksLikeComponent? airTankLooksLike))
            return;

        string PopupTextLoc = _localization.GetString(PopupText, ("state", airTankLooksLike.Contains));
        string ConfirmTextLoc = _localization.GetString(ConfirmText);

        _popupSystem.PopupPredicted(PopupTextLoc + "\n" + ConfirmTextLoc, null, user, user, PopupType.MediumCaution);
    }

    public bool CheckShouldBreathe(EntityUid user, EntityUid tank)
    {
        if (!TryComp(user, out AirTankShouldBreatheComponent? shouldBreathe) ||
            !TryComp(tank, out AirTankLooksLikeComponent? looksLike))
            return true;

        if (shouldBreathe.TankTypes.Contains(looksLike.Contains))
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
