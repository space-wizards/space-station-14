using Content.Server.Actions; //Starlight
using Content.Server.Humanoid;
using Content.Shared.Cloning.Events;
using Content.Shared._Starlight.Humanoid.Markings;
using Content.Shared.Actions;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Mobs;
using Content.Shared.Toggleable;
using Content.Shared.Wagging;
using Robust.Shared.Prototypes;

namespace Content.Server.Wagging;

/// <summary>
/// Adds an action to toggle wagging animation for tails markings that supporting this
/// </summary>
public sealed class WaggingSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoidAppearance = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    [Dependency] private readonly StarlightMarkingSystem _starlightMarking = default!; //starlight edit

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WaggingComponent, ComponentShutdown>(OnWaggingShutdown);
        SubscribeLocalEvent<WaggingComponent, ToggleActionEvent>(OnWaggingToggle);
        SubscribeLocalEvent<WaggingComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<WaggingComponent, CloningEvent>(OnCloning);
    }

    private void OnCloning(Entity<WaggingComponent> ent, ref CloningEvent args)
    {
        if (!args.Settings.EventComponents.Contains(Factory.GetRegistration(ent.Comp.GetType()).Name))
            return;

        EnsureComp<WaggingComponent>(args.CloneUid);
    }

    private void OnWaggingShutdown(EntityUid uid, WaggingComponent component, ComponentShutdown args)
    {
        _actions.RemoveAction(uid, component.ActionEntity);
    }

    private void OnWaggingToggle(EntityUid uid, WaggingComponent component, ref ToggleActionEvent args)
    {
        if (args.Handled)
            return;

        TryToggleWagging(uid, wagging: component);
    }

    private void OnMobStateChanged(EntityUid uid, WaggingComponent component, MobStateChangedEvent args)
    {
        if (component.Wagging)
            TryToggleWagging(uid, wagging: component);
    }

    public bool TryToggleWagging(EntityUid uid, WaggingComponent? wagging = null, HumanoidAppearanceComponent? humanoid = null)
    {
        if (!Resolve(uid, ref wagging, ref humanoid))
            return false;

        if (!humanoid.MarkingSet.Markings.TryGetValue(MarkingCategories.Tail, out var markings))
            return false;

        if (markings.Count == 0)
            return false;

        wagging.Wagging = !wagging.Wagging;

        // starlight start
        string? target;
        if (wagging.Wagging)
        {
            _starlightMarking.TryGetWaggingId(markings[0].MarkingId, out target);
        }
        else
        {
            _starlightMarking.TryGetStaticId(markings[0].MarkingId, out target);
        }

        if (target == null)
        {
            Log.Error($"Unable to find corresponding wagging or static ID for {markings[0].MarkingId}?");
            return false;
        }

        _humanoidAppearance.SetMarkingId(uid, MarkingCategories.Tail, 0, target,
            humanoid: humanoid);
        // starlight end

        return true;
    }
}
