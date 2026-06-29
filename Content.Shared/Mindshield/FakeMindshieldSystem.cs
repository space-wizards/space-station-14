using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Clothing;
using Content.Shared.Implants;
using Content.Shared.Inventory;
using Content.Shared.Mindshield.Components;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Mindshield;

public sealed partial class FakeMindShieldSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedMindShieldSystem _mindShields = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Other events
        SubscribeLocalEvent<FakeMindShieldComponent, ChameleonControllerOutfitSelectedEvent>(OnChameleonControllerOutfitSelected);

        // Toggle events
        SubscribeLocalEvent<FakeMindShieldComponent, FakeMindShieldToggleEvent>(OnToggleMindshield);
        SubscribeLocalEvent<FakeMindShieldComponent, InventoryRelayedEvent<FakeMindShieldToggleEvent>>((e, ref sk) => OnToggleMindshield(e, ref sk.Args));
        SubscribeLocalEvent<FakeMindShieldComponent, ImplantRelayEvent<FakeMindShieldToggleEvent>>((e, ref sk) => OnToggleMindshield(e, ref sk.Args));

        // Mindshield events
        SubscribeLocalEvent<FakeMindShieldComponent, ImplantRelayEvent<GetMindShieldStatusEvent>>((a, ref k) => OnQueryStatus(a, ref k.Args));
        SubscribeLocalEvent<FakeMindShieldComponent, InventoryRelayedEvent<GetMindShieldStatusEvent>>((a, ref k) => OnQueryStatus(a, ref k.Args));
        SubscribeLocalEvent<FakeMindShieldComponent, GetMindShieldStatusEvent>(OnQueryStatus);

        // these five events are to manage the mindshield's dirtying
        SubscribeLocalEvent<FakeMindShieldComponent, ImplantImplantedEvent>(OnFakeMindshieldImplanted);
        SubscribeLocalEvent<FakeMindShieldComponent, ImplantRemovedEvent>(OnFakeMindshieldImplantRemoved);
        SubscribeLocalEvent<FakeMindShieldComponent, ComponentRemove>(OnFakeMindshieldRemoved);
        SubscribeLocalEvent<FakeMindShieldComponent, ClothingGotEquippedEvent>(OnFakeMindshieldEquip);
        SubscribeLocalEvent<FakeMindShieldComponent, ClothingGotUnequippedEvent>(OnFakeMindshieldUnequip);

        // Innate things
        SubscribeLocalEvent<FakeMindShieldComponent, MapInitEvent>(OnMapInit);
    }

    private void OnFakeMindshieldEquip(Entity<FakeMindShieldComponent> ent, ref ClothingGotEquippedEvent args)
    {
        _mindShields.RefreshMindshieldStatus(args.Wearer);
    }

    private void OnFakeMindshieldUnequip(Entity<FakeMindShieldComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        _mindShields.RefreshMindshieldStatus(args.Wearer);
    }

    private void OnFakeMindshieldRemoved(Entity<FakeMindShieldComponent> ent, ref ComponentRemove args)
    {
        if (!ent.Comp.IsInnate)
            return;
        _mindShields.RefreshMindshieldStatus(ent.Owner);
    }

    private void OnFakeMindshieldImplantRemoved(Entity<FakeMindShieldComponent> ent, ref ImplantRemovedEvent args)
    {
        _mindShields.RefreshMindshieldStatus(args.Implanted);
    }

    private void OnFakeMindshieldImplanted(Entity<FakeMindShieldComponent> ent, ref ImplantImplantedEvent args)
    {
        _mindShields.RefreshMindshieldStatus(args.Implanted);
    }

    private void OnMapInit(Entity<FakeMindShieldComponent> ent, ref MapInitEvent args)
    {
        if (!ent.Comp.IsInnate)
            return;

        _actions.AddAction(ent.Owner, ent.Comp.Action);
        _mindShields.RefreshMindshieldStatus(ent.Owner);
    }

    private void OnQueryStatus(Entity<FakeMindShieldComponent> ent, ref GetMindShieldStatusEvent args)
    {
        args.IsVisible |= ent.Comp.IsEnabled;
    }

    private void OnToggleMindshield(Entity<FakeMindShieldComponent> ent, ref FakeMindShieldToggleEvent args)
    {
        if (args.ActionTag != ent.Comp.ActionTag)
            return;

        ent.Comp.IsEnabled = !ent.Comp.IsEnabled;
        args.Toggle = true;
        args.Handled = true;
        Dirty(ent.Owner, ent.Comp);
        _mindShields.RefreshMindshieldStatus(args.Performer);
    }

    private void OnChameleonControllerOutfitSelected(EntityUid uid, FakeMindShieldComponent component, ChameleonControllerOutfitSelectedEvent args)
    {
        if (!component.ChameleonControllable)
            return;

        if (component.IsEnabled == args.ChameleonOutfit.HasMindShield)
            return;

        // This assumes there is only one fake mindshield action per entity (This is currently enforced)
        if (!TryComp<ActionsComponent>(uid, out var actionsComp))
            return;

        // In case the fake mindshield ever doesn't have an action.
        var actionFound = false;

        foreach (var action in actionsComp.Actions)
        {
            if (!_tag.HasTag(action, component.ActionTag))
                continue;

            if (!TryComp<ActionComponent>(action, out var actionComp))
                continue;

            actionFound = true;

            if (_actions.IsCooldownActive(actionComp, _timing.CurTime))
                continue;

            component.IsEnabled = args.ChameleonOutfit.HasMindShield;
            _actions.SetToggled(action, args.ChameleonOutfit.HasMindShield);
            Dirty(uid, component);

            if (actionComp.UseDelay != null)
                _actions.SetCooldown(action, actionComp.UseDelay.Value);

            return;
        }

        // If they don't have the action for some reason, still set it correctly.
        if (!actionFound)
        {
            component.IsEnabled = args.ChameleonOutfit.HasMindShield;
            Dirty(uid, component);
        }
    }
}

/// <summary>
/// Inventory relayed event telling all fake mindshields with the same ActionTag to try to toggle themselves.
/// </summary>
public sealed partial class FakeMindShieldToggleEvent : InstantActionEvent, IInventoryRelayEvent
{
    public SlotFlags TargetSlots => SlotFlags.WITHOUT_POCKET;

    /// <summary>
    /// The tag the fake mindshield needs to have to be successfully toggled.
    /// We seperate it out to allow an entity to, for example, have an implant one and a store bought one without conflicts.
    /// </summary>
    [DataField]
    public ProtoId<TagPrototype> ActionTag = "FakeMindShieldImplant";
}
