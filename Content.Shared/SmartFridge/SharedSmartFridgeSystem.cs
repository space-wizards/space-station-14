using Content.Shared.Access.Systems;
using Content.Shared.Construction.EntitySystems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Storage.Components;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.SmartFridge;

public abstract class SharedSmartFridgeSystem : EntitySystem
{
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SmartFridgeComponent, InteractUsingEvent>(OnInteractUsing, after: [typeof(AnchorableSystem)]);
        SubscribeLocalEvent<SmartFridgeComponent, EntRemovedFromContainerMessage>(OnItemRemoved);

        SubscribeLocalEvent<SmartFridgeComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<SmartFridgeComponent, ComponentHandleState>(OnHandleState);

        SubscribeLocalEvent<SmartFridgeComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAltVerb);
        SubscribeLocalEvent<SmartFridgeComponent, GetDumpableVerbEvent>(OnGetDumpableVerb);
        SubscribeLocalEvent<SmartFridgeComponent, DumpEvent>(OnDump);

        Subs.BuiEvents<SmartFridgeComponent>(SmartFridgeUiKey.Key,
            sub =>
            {
                sub.Event<SmartFridgeDispenseItemMessage>(OnDispenseItem);
            });
    }

    private bool DoInsert(Entity<SmartFridgeComponent> ent, EntityUid user, IEnumerable<EntityUid> usedItems, bool playSound)
    {
        if (!_container.TryGetContainer(ent, ent.Comp.Container, out var container))
            return false;

        if (!Allowed(ent, user))
            return true;

        bool anyInserted = false;
        foreach (var used in usedItems)
        {
            if (!_whitelist.CheckBoth(used, ent.Comp.Blacklist, ent.Comp.Whitelist))
                continue;
            anyInserted = true;

            _container.Insert(used, container);
            var key = new SmartFridgeEntry(Identity.Name(used, EntityManager));
            if (!ent.Comp.Entries.Contains(key))
                ent.Comp.Entries.Add(key);

            ent.Comp.ContainedEntries.TryAdd(key, new());
            var entries = ent.Comp.ContainedEntries[key];
            if (!entries.Contains(used))
                entries.Add(used);

            Dirty(ent);
            UpdateUI(ent);
        }

        if (anyInserted && playSound)
        {
            _audio.PlayPredicted(ent.Comp.InsertSound, ent, user);
        }

        return anyInserted;
    }

    private void OnInteractUsing(Entity<SmartFridgeComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !_hands.CanDrop(args.User, args.Used))
            return;

        args.Handled = DoInsert(ent, args.User, [args.Used], true);
    }

    private void OnItemRemoved(Entity<SmartFridgeComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        var key = new SmartFridgeEntry(Identity.Name(args.Entity, EntityManager));

        if (ent.Comp.ContainedEntries.TryGetValue(key, out var contained))
        {
            contained.Remove(args.Entity);
        }

        Dirty(ent);
        UpdateUI(ent);
    }

    private bool Allowed(Entity<SmartFridgeComponent> machine, EntityUid user)
    {
        if (_accessReader.IsAllowed(user, machine))
            return true;

        _popup.PopupPredicted(Loc.GetString("smart-fridge-component-try-eject-access-denied"), machine, user);
        _audio.PlayPredicted(machine.Comp.SoundDeny, machine, user);
        return false;
    }

    private void OnDispenseItem(Entity<SmartFridgeComponent> ent, ref SmartFridgeDispenseItemMessage args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (!Allowed(ent, args.Actor))
            return;

        if (!ent.Comp.ContainedEntries.TryGetValue(args.Entry, out var contained))
        {
            _audio.PlayPredicted(ent.Comp.SoundDeny, ent, args.Actor);
            _popup.PopupPredicted(Loc.GetString("smart-fridge-component-try-eject-unknown-entry"), ent, args.Actor);
            return;
        }

        foreach (var item in contained)
        {
            if (!_container.TryRemoveFromContainer(item))
                continue;

            _audio.PlayPredicted(ent.Comp.SoundVend, ent, args.Actor);
            contained.Remove(item);
            Dirty(ent);
            UpdateUI(ent);
            return;
        }

        _audio.PlayPredicted(ent.Comp.SoundDeny, ent, args.Actor);
        _popup.PopupPredicted(Loc.GetString("smart-fridge-component-try-eject-out-of-stock"), ent, args.Actor);
    }

    private void OnGetAltVerb(Entity<SmartFridgeComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        var user = args.User;

        if (!args.CanInteract
            || args.Using is not { } item
            || !_hands.CanDrop(user, item)
            || !_whitelist.CheckBoth(item, ent.Comp.Blacklist, ent.Comp.Whitelist))
            return;

        args.Verbs.Add(new AlternativeVerb
        {
            Act = () => DoInsert(ent, user, [item], true),
            Text = Loc.GetString("verb-categories-insert"),
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/insert.svg.192dpi.png")),
        });
    }

    private void OnGetDumpableVerb(Entity<SmartFridgeComponent> ent, ref GetDumpableVerbEvent args)
    {
        if (_accessReader.IsAllowed(args.User, ent))
        {
            args.Verb = Loc.GetString("dump-smartfridge-verb-name", ("unit", ent));
        }
    }

    private void OnDump(Entity<SmartFridgeComponent> ent, ref DumpEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        args.PlaySound = true;

        DoInsert(ent, args.User, args.DumpQueue, false);
    }

    private void OnGetState(Entity<SmartFridgeComponent> ent, ref ComponentGetState args)
    {
        var state = new SmartFridgeComponentState();
        state.Entries = ent.Comp.Entries;
        state.ContainedEntries = new(ent.Comp.ContainedEntries.Count);

        foreach (var (key, value) in ent.Comp.ContainedEntries)
        {
            var set = new HashSet<NetEntity>(value.Count);
            foreach (var entity in value)
            {
                set.Add(GetNetEntity(entity));
            }
            state.ContainedEntries[key] = set;
        }

        args.State = state;
    }

    private void OnHandleState(Entity<SmartFridgeComponent> ent, ref ComponentHandleState args)
    {
        if (args.Current is not SmartFridgeComponentState state)
            return;

        ent.Comp.Entries = state.Entries;
        ent.Comp.ContainedEntries = new(state.ContainedEntries.Count);

        foreach (var (key, value) in state.ContainedEntries)
        {
            var set = new HashSet<EntityUid>(value.Count);
            foreach (var entity in value)
            {
                if (TryGetEntity(entity, out var uid))
                    set.Add(uid.Value);
            }
            ent.Comp.ContainedEntries[key] = set;
        }

        UpdateUI(ent);
    }

    protected virtual void UpdateUI(Entity<SmartFridgeComponent> ent)
    {

    }

    [Serializable, NetSerializable]
    private sealed class SmartFridgeComponentState : ComponentState
    {
        public List<SmartFridgeEntry> Entries = default!;
        public Dictionary<SmartFridgeEntry, HashSet<NetEntity>> ContainedEntries = default!;
    }
}
