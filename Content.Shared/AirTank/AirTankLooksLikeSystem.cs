using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared.AirTank;

public sealed class AirTankLooksLikeSystem : EntitySystem
{
    [Dependency] private readonly ILocalizationManager _localization = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly INetManager _netMan = default!;

    public readonly string PopupPrefix = "air-tank-looks-like-";
    public readonly string ConfirmText = "air-tank-looks-like-confirm";

    public void CreateShouldntBreathePopup(EntityUid user, EntityUid tank)
    {
        if (_netMan.IsServer)
            return;

        if (!TryComp(tank, out AirTankLooksLikeComponent? airTankLooksLike))
            return;

        string PopupTextLoc = _localization.GetString(PopupPrefix + airTankLooksLike.Contains.ToString().ToLower());
        string ConfirmTextLoc = _localization.GetString(ConfirmText);

        _popupSystem.PopupEntity(PopupTextLoc + "\n" + ConfirmTextLoc, user, PopupType.MediumCaution);
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
