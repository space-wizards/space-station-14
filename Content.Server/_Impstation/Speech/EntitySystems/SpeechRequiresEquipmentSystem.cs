using Content.Server.Speech.Components;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Speech;
using Content.Shared.Whitelist;

namespace Content.Server.Speech.EntitySystems;

public sealed partial class SpeechRequiresEquipmentSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpeechRequiresEquipmentComponent, SpeakAttemptEvent>(OnSpeechAttempt);
    }

    public void OnSpeechAttempt(Entity<SpeechRequiresEquipmentComponent> ent, ref SpeakAttemptEvent args)
    {
        var canSpeak = true;

        foreach (var (slot, whitelist) in ent.Comp.Equipment)
        {
            if (!_inventory.TryGetSlotEntity(ent, slot, out var item))
            {
                canSpeak = false;
                break;
            }

            if (_whitelist.IsWhitelistFail(whitelist, item.Value))
            {
                canSpeak = false;
                break;
            }
        }

        // TODO: SpeakAttemptEvent should be modified to include an optional LocId
        // reason for why the speak attempt was cancelled.
        if (!canSpeak)
        {
            args.Cancel();
            if (ent.Comp.FailMessage != null)
            {
                _popup.PopupEntity(Loc.GetString(ent.Comp.FailMessage), ent, ent);
            }
        }
    }
}
