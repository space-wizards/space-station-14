// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Inventory.Events;
using Content.Server.Chat.Systems;
using Content.Shared.Inventory;
using Content.Server.Administration.Logs;
using Content.Shared.Database;
using Timer = Robust.Shared.Timing.Timer;
using Content.Shared.Forensics.Components;
using Content.Shared.Forensics;
using Content.Server.Popups;
using Content.Shared.Actions;
using Content.Shared.DeadSpace.HardsuitIdentification;
using Content.Shared.Interaction.Components;
using Content.Server.Speech.EntitySystems;
using Content.Shared.Chat;
using Content.Shared.Emag.Systems;
using Robust.Shared.Audio.Systems;
using Content.Shared.Speech.Components;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Zombies;
using Content.Shared.DeadSpace.Necromorphs.InfectionDead;

namespace Content.Server.DeadSpace.HardsuitIdentification;

public sealed class HardsuitIdentificationSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly VocalSystem _vocal = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedSubdermalImplantSystem _implants = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<HardsuitIdentificationComponent, GotEquippedEvent>(OnEquip);
        SubscribeLocalEvent<HardsuitIdentificationComponent, GotUnequippedEvent>(OnUnequip);
        SubscribeLocalEvent<HardsuitIdentificationComponent, GetItemActionsEvent>(OnGetActions);
        SubscribeLocalEvent<HardsuitIdentificationComponent, StoreDNAActionEvent>(OnDNAStore);
        SubscribeLocalEvent<HardsuitIdentificationComponent, GotEmaggedEvent>(OnEmagged);
        SubscribeLocalEvent<DnaComponent, GenerateDnaEvent>(OnDnaChanged);
        SubscribeLocalEvent<ZombieComponent, ComponentStartup>(OnZombieStartup);
        SubscribeLocalEvent<DnaComponent, NecroficationStartedEvent>(OnNecroficationStarted);
    }

    public void OnEquip(EntityUid uid, HardsuitIdentificationComponent comp, GotEquippedEvent args)
    {
        if (comp.Activated == true || comp.DNAWasStored == false)
            return;

        if (TryComp(args.Equipee, out DnaComponent? dna) && comp.DNA == dna.DNA)
            return;

        ActivateProtection((uid, comp), args.Equipee, args.Slot);
    }

    private void OnUnequip(EntityUid uid, HardsuitIdentificationComponent comp, GotUnequippedEvent args)
    {
        if (!comp.Activated)
            return;

        TriggerPunishment(args.Equipee, comp);
    }

    private void OnDnaChanged(Entity<DnaComponent> ent, ref GenerateDnaEvent args)
    {
        TryActivateEquippedItems(ent.Owner, requireDnaMismatch: true);
    }

    private void OnZombieStartup(Entity<ZombieComponent> ent, ref ComponentStartup args)
    {
        TryActivateEquippedItems(ent.Owner, requireDnaMismatch: false);
    }

    private void OnNecroficationStarted(Entity<DnaComponent> ent, ref NecroficationStartedEvent args)
    {
        TryActivateEquippedItems(ent.Owner, requireDnaMismatch: false);
    }

    private void TryActivateEquippedItems(EntityUid wearer, bool requireDnaMismatch)
    {
        var slots = _inventory.GetSlotEnumerator(wearer);
        while (slots.NextItem(out var equipment, out var slot))
        {
            if (!TryComp<HardsuitIdentificationComponent>(equipment, out var comp) ||
                !comp.DNAWasStored ||
                comp.Activated ||
                !comp.DissolveOnDnaChange)
            {
                continue;
            }

            if (requireDnaMismatch && TryComp<DnaComponent>(wearer, out var dna) && comp.DNA == dna.DNA)
                continue;

            ActivateProtection((equipment, comp), wearer, slot.Name);

            // One acidifier is sufficient, but every nonlethal protected item should be removed.
            if (!comp.Nonlethal)
                return;
        }
    }

    private void ActivateProtection(Entity<HardsuitIdentificationComponent> equipment, EntityUid wearer, string slot)
    {
        if (!Exists(wearer))
            return;

        var (uid, comp) = equipment;
        _audio.PlayPvs(comp.WrongOwnerSound, uid);

        if (comp.Nonlethal)
        {
            Timer.Spawn(0,
                () =>
                {
                    _popupSystem.PopupEntity(
                        Loc.GetString("hardsuit-identification-error"),
                        wearer,
                        wearer);
                    _inventory.TryUnequip(wearer, slot, true, true);
                });
            return;
        }

        comp.Activated = true;

        _adminLogger.Add(LogType.Trigger, LogImpact.Medium,
            $"{ToPrettyString(wearer):user} activated hardsuit acidification system of {ToPrettyString(uid):target}");

        EnsureComp<UnremoveableComponent>(uid);

        comp.PunishmentImplantEntity = _implants.AddImplant(wearer, comp.PunishmentImplant);

        _popupSystem.PopupEntity(
            Loc.GetString("hardsuit-identification-error-spikes"),
            wearer,
            wearer,
            Shared.Popups.PopupType.Large);

        Timer.Spawn(1000,
            () => _chat.TrySendInGameICMessage(uid,
                Loc.GetString("hardsuit-identification-error"),
                InGameICChatType.Speak, true));

        Timer.Spawn(1500,
            () => { if (TryComp(wearer, out VocalComponent? v)) _vocal.TryPlayScreamSound(wearer, v); });

        Timer.Spawn(2000,
            () => _chat.TrySendInGameICMessage(uid, "3", InGameICChatType.Speak, true));

        Timer.Spawn(2500,
            () => { if (TryComp(wearer, out VocalComponent? v)) _vocal.TryPlayScreamSound(wearer, v); });

        Timer.Spawn(3000,
            () => _chat.TrySendInGameICMessage(uid, "2", InGameICChatType.Speak, true));

        Timer.Spawn(3500,
            () => { if (TryComp(wearer, out VocalComponent? v)) _vocal.TryPlayScreamSound(wearer, v); });

        Timer.Spawn(4000,
            () =>
            {
                _chat.TrySendInGameICMessage(uid, "1", InGameICChatType.Speak, true);
                if (TryComp(wearer, out VocalComponent? v)) _vocal.TryPlayScreamSound(wearer, v);
            });

        Timer.Spawn(5000,
            () => TriggerPunishment(wearer, comp));
    }

    private void TriggerPunishment(EntityUid wearer, HardsuitIdentificationComponent comp)
    {
        if (comp.PunishmentTriggered ||
            comp.PunishmentImplantEntity is not { } implant ||
            !Exists(implant) ||
            !Exists(wearer))
        {
            return;
        }

        comp.PunishmentTriggered = true;
        var ev = new ActivateImplantEvent { Performer = wearer };
        RaiseLocalEvent(implant, ev);
    }

    private void OnGetActions(EntityUid uid, HardsuitIdentificationComponent comp, GetItemActionsEvent args)
    {
        if (comp.DNAWasStored == false)
        {
            args.AddAction(ref comp.ActionEntity, comp.Action);
        }
    }

    public void OnDNAStore(EntityUid uid, HardsuitIdentificationComponent comp, StoreDNAActionEvent args)
    {
        if (args.Handled)
            return;

        if (comp.DNAWasStored == true)
        {
            _popupSystem.PopupEntity(Loc.GetString("hardsuit-identification-dna-already-stored"), args.Performer, args.Performer);
        }
        else
        {
            if (TryComp(args.Performer, out DnaComponent? dna) && dna.DNA != null)
            {
                comp.DNA = dna.DNA;
                comp.DNAWasStored = true;

                _popupSystem.PopupEntity(Loc.GetString("hardsuit-identification-dna-was-stored"), args.Performer, args.Performer);
            }
            else
            {
                _popupSystem.PopupEntity(Loc.GetString("hardsuit-identification-dna-not-presented"), args.Performer, args.Performer);
            }
        }

        args.Handled = true;
    }

    public void OnEmagged(EntityUid uid, HardsuitIdentificationComponent comp, GotEmaggedEvent args)
    {
        if (!comp.CanEmag)
            return;
    
        _audio.PlayPvs(comp.SparkSound, uid);
    
        if (comp.Activated)
        {
            _popupSystem.PopupEntity(Loc.GetString("hardsuit-identification-on-emagged-late"), uid);
        }
        else
        {
            _popupSystem.PopupEntity(Loc.GetString("hardsuit-identification-on-emagged"), uid);
        }

        RemComp<HardsuitIdentificationComponent>(uid);
    
        args.Handled = true;
    }
}
