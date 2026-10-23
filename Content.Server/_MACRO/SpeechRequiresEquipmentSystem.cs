using Content.Server._MACRO.Speech.Components;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Speech;
using Content.Shared.Whitelist;

namespace Content.Server._MACRO.Speech.EntitySystems;

public sealed partial class SpeechRequiresEquipmentSystem : EntitySystem
{
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpeechRequiresEquipmentComponent, SpeakAttemptEvent>(OnSpeechAttempt);
    }

    public void OnSpeechAttempt(Entity<SpeechRequiresEquipmentComponent> ent, ref SpeakAttemptEvent args)
    {
        if (_inventory.TryGetContainerSlotEnumerator(ent.Owner, out var enumerator, SlotFlags.WITHOUT_POCKET))
        {
            while (enumerator.NextItem(out var item, out _))
            {
                if (TryComp<SpeechSoundComponent>(item, out var comp)
                    && _whitelist.CheckBoth(item, ent.Comp.Blacklist, ent.Comp.Whitelist))
                    return;
            }
        }

        args.Cancel();
        if (ent.Comp.FailMessage != null)
            _popup.PopupEntity(Loc.GetString(ent.Comp.FailMessage), ent, ent);
    }
}
