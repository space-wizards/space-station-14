using Content.Server.Bible.Components;
using Content.Server.Popups;
using Content.Shared.Item;
using Content.Shared.Inventory.Events;
using Content.Shared.Popups;
using Content.Shared._Tinystation.Knight.Components;

namespace Content.Server._Tinystation.Knight.Systems;

public sealed class RequireBibleUserSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RequireBibleUserComponent, GettingPickedUpAttemptEvent>(OnPickupAttempt);
        SubscribeLocalEvent<RequireBibleUserComponent, BeingEquippedAttemptEvent>(OnEquipAttempt);
    }

    private void OnPickupAttempt(EntityUid uid, RequireBibleUserComponent component, GettingPickedUpAttemptEvent args)
    {
        if (!HasComp<BibleUserComponent>(args.User))
        {
            _popup.PopupEntity(Loc.GetString("knight-weapon-rejected"), args.User, args.User, PopupType.SmallCaution);
            args.Cancel();
        }
    }

    private void OnEquipAttempt(EntityUid uid, RequireBibleUserComponent component, BeingEquippedAttemptEvent args)
    {
        if (!HasComp<BibleUserComponent>(args.EquipTarget))
        {
            _popup.PopupEntity(Loc.GetString("knight-weapon-rejected"), args.EquipTarget, args.EquipTarget, PopupType.SmallCaution);
            args.Cancel();
        }
    }
}
