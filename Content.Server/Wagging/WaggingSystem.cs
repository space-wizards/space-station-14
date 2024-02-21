using Content.Server.Actions;
using Content.Server.Humanoid;
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

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WaggingComponent, MapInitEvent>(OnWaggingMapInit);
        SubscribeLocalEvent<WaggingComponent, ComponentShutdown>(OnWaggingShutdown);
        SubscribeLocalEvent<WaggingComponent, ToggleActionEvent>(OnWaggingToggle);
        SubscribeLocalEvent<WaggingComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnWaggingMapInit(EntityUid uid, WaggingComponent component, MapInitEvent args)
    {
        _actions.AddAction(uid, ref component.ActionEntity, component.Action, uid);
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

        for (var idx = 0; idx < markings.Count; idx++) // Animate all possible tails
        {
            var currentMarkingId = markings[idx].MarkingId;
            string newMarkingId;

            if (wagging.Wagging)
            {
                newMarkingId = $"{currentMarkingId}{wagging.Suffix}";
            }
            else
            {
                if (currentMarkingId.EndsWith(wagging.Suffix))
                {
                    newMarkingId = currentMarkingId[..^wagging.Suffix.Length];
                }
                else
                {
                    newMarkingId = currentMarkingId;
                    Log.Warning($"Unable to revert wagging for {currentMarkingId}");
                }
            }

            if (!_prototype.HasIndex<MarkingPrototype>(newMarkingId))
            {
                Log.Warning($"{ToPrettyString(uid)} tried toggling wagging but {newMarkingId} marking doesn't exist");
                continue;
            }

            _humanoidAppearance.SetMarkingId(uid, MarkingCategories.Tail, idx, newMarkingId,
                humanoid: humanoid);
        }

        return true;
    }
}
