using Content.Server.Administration.Managers;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Clothing.Components;
using Content.Server.Mind.Components;
using Content.Server.Popups;
using Content.Shared.Clothing.Components;
using Content.Shared.Inventory.Events;
using Content.Shared.Popups;
using Robust.Shared.Player;

namespace Content.Server.Clothing.Systems;

public sealed class RestrictedClothingSystem : EntitySystem
{
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly FlammableSystem _flammableSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RestrictedClothingComponent, GotEquippedEvent>(OnGotEquipped);
    }

    private void OnGotEquipped(EntityUid uid, RestrictedClothingComponent component, GotEquippedEvent args)
    {
        if (!TryComp(uid, out ClothingComponent? clothing))
            return;

        // check if entity was actually used as clothing
        // not just taken in pockets or something
        if (!clothing.Slots.HasFlag(args.SlotFlags))
        	return;

        if (!TryComp(args.Equipee, out MindComponent? mind))
            return;

        if (args.Equipee.GetHashCode() == component.WhitelistedUid)
            return;

        if (!component.RequireWhitelist && mind.HasMind && mind.Mind!.Session != null &&
            _adminManager.HasAdminFlag(mind.Mind!.Session!, component.Permissions, true))
            return;

        _flammableSystem.AdjustFireStacks(args.Equipee, 5);
        _flammableSystem.Ignite(args.Equipee);

        var xform = Transform(args.Equipee);
        _popupSystem.PopupEntity(Loc.GetString("admin-smite-set-alight-self"), args.Equipee,
            args.Equipee, PopupType.LargeCaution);
        _popupSystem.PopupCoordinates(Loc.GetString("admin-smite-set-alight-others", ("name", args.Equipee)), xform.Coordinates,
            Filter.PvsExcept(args.Equipee), true, PopupType.MediumCaution);

        QueueDel(uid);
    }
}
