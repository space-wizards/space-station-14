using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Implants;
using Content.Shared.Inventory;
using Content.Shared.Mindshield.Components;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Mindshield.FakeMindshield;

public sealed class FakeMindShieldSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        // Other events
        SubscribeLocalEvent<FakeMindShieldComponent, ChameleonControllerOutfitSelectedEvent>(OnChameleonControllerOutfitSelected);

        // Toggle events
        SubscribeLocalEvent<FakeMindShieldComponent, FakeMindShieldToggleEvent>(OnToggleMindshield);
        SubscribeLocalEvent<FakeMindShieldComponent, InventoryRelayedEvent<FakeMindShieldToggleEvent>>((e, ref sk) => OnToggleMindshield(e.Owner, e.Comp, sk.Args));
        SubscribeLocalEvent<FakeMindShieldComponent, ImplantRelayEvent<FakeMindShieldToggleEvent>>((e, ref sk) => OnToggleMindshield(e.Owner, e.Comp, sk.Args));
        // Visuals events
        SubscribeLocalEvent<FakeMindShieldComponent, ImplantRelayEvent<QueryMindshieldVisualsEvent>>((a, ref k) => OnQueryFakeMindShieldVisuals(a, ref k.Args));
        SubscribeLocalEvent<FakeMindShieldComponent, InventoryRelayedEvent<QueryMindshieldVisualsEvent>>((a, ref k) => OnQueryFakeMindShieldVisuals(a, ref k.Args));
        SubscribeLocalEvent<FakeMindShieldComponent, QueryMindshieldVisualsEvent>(OnQueryFakeMindShieldVisuals);
    }

    private void OnQueryFakeMindShieldVisuals(Entity<FakeMindShieldComponent> ent, ref QueryMindshieldVisualsEvent args)
    {
        args.IsVisible |= ent.Comp.IsEnabled;
        // Apply the visuals. We check the priority so that this fake mindshield should almost always get overwritten by a real mindshield.
        if (ent.Comp.VisualPriority > args.Priority && ent.Comp.IsEnabled)
        {
            args.Priority = ent.Comp.VisualPriority;
            args.MindshieldStatusIcon = ent.Comp.MindshieldStatusIcon;
        }
    }

    private void OnToggleMindshield(EntityUid uid, FakeMindShieldComponent comp, FakeMindShieldToggleEvent args)
    {
        if (args.ActionTag != comp.ActionTag)
            return;
        comp.IsEnabled = !comp.IsEnabled;
        args.Toggle = true;
        args.Handled = true;
        Dirty(uid, comp);
    }

    private void OnChameleonControllerOutfitSelected(EntityUid uid, FakeMindShieldComponent component, ChameleonControllerOutfitSelectedEvent args)
    {
        if (!component.ChameleonControllable)
            return;

        if (component.IsEnabled == args.ChameleonOutfit.HasMindshield)
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

            component.IsEnabled = args.ChameleonOutfit.HasMindshield;
            _actions.SetToggled(action, args.ChameleonOutfit.HasMindshield);
            Dirty(uid, component);

            if (actionComp.UseDelay != null)
                _actions.SetCooldown(action, actionComp.UseDelay.Value);

            return;
        }

        // If they don't have the action for some reason, still set it correctly.
        if (!actionFound)
        {
            component.IsEnabled = args.ChameleonOutfit.HasMindshield;
            Dirty(uid, component);
        }
    }
}

public sealed partial class FakeMindShieldToggleEvent : InstantActionEvent, IInventoryRelayEvent
{
    public SlotFlags TargetSlots => SlotFlags.All;
    [DataField]
    public ProtoId<TagPrototype> ActionTag = "FakeMindShieldImplant";
}
