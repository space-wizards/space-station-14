using Content.Shared.Actions;
using Content.Shared.Cloning.Events;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Mobs;
using Content.Shared.Toggleable;
using Robust.Shared.Prototypes;

namespace Content.Shared.Wagging;

/// <summary>
/// Adds an action to toggle the wagging animation for tail markings that support it
/// </summary>
public sealed class WaggingSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoidAppearance = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WaggingComponent, MapInitEvent>(OnWaggingMapInit);
        SubscribeLocalEvent<WaggingComponent, ComponentShutdown>(OnWaggingShutdown);
        SubscribeLocalEvent<WaggingComponent, ToggleActionEvent>(OnWaggingToggle);
        SubscribeLocalEvent<WaggingComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<WaggingComponent, CloningEvent>(OnCloning);
    }

    private void OnWaggingMapInit(Entity<WaggingComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent, ref ent.Comp.ActionEntity, ent.Comp.Action, ent);
        DirtyField(ent, ent.Comp, nameof(WaggingComponent.ActionEntity));
    }

    private void OnWaggingShutdown(Entity<WaggingComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Comp.ActionEntity);
        DirtyField(ent, ent.Comp, nameof(WaggingComponent.ActionEntity));
    }

    private void OnWaggingToggle(Entity<WaggingComponent> ent, ref ToggleActionEvent args)
    {
        if (args.Handled)
            return;

        TrySetWagging(ent.AsNullable(), !ent.Comp.Wagging);
        args.Handled = true;
    }

    private void OnMobStateChanged(Entity<WaggingComponent> ent, ref MobStateChangedEvent args)
    {
        TrySetWagging(ent.AsNullable(), false);
    }

    private void OnCloning(Entity<WaggingComponent> ent, ref CloningEvent args)
    {
        if (!args.Settings.EventComponents.Contains(Factory.GetRegistration(ent.Comp.GetType()).Name))
            return;

        EnsureComp<WaggingComponent>(args.CloneUid);
    }

    /// <summary>
    /// Returns whether the passed entity is wagging its tail.
    /// </summary>
    public bool IsWagging(Entity<WaggingComponent?> ent)
    {
        return Resolve(ent, ref ent.Comp, false) && ent.Comp.Wagging;
    }

    /// <summary>
    /// Stops or starts the wagging of entity tails.
    /// </summary>
    public bool TrySetWagging(Entity<WaggingComponent?, HumanoidAppearanceComponent?> ent, bool value)
    {
        if (!Resolve(ent, ref ent.Comp1, ref ent.Comp2))
            return false;

        if (ent.Comp1.Wagging == value)
            return false;

        if (!ent.Comp2.MarkingSet.Markings.TryGetValue(MarkingCategories.Tail, out var markings))
            return false;

        if (markings.Count == 0)
            return false;

        ent.Comp1.Wagging = value;
        DirtyField(ent, ent.Comp1, nameof(WaggingComponent.Wagging));

        // Animate all possible tails
        for (var idx = 0; idx < markings.Count; idx++)
        {
            var currentMarkingId = markings[idx].MarkingId;

            string newMarkingId;
            if (value)
            {
                // Tail is already wagging, skipping
                if (currentMarkingId.EndsWith(ent.Comp1.Suffix))
                    continue;

                newMarkingId = $"{currentMarkingId}{ent.Comp1.Suffix}";
            }
            else
            {
                // Tail is already not wagging, skipping
                if (!currentMarkingId.EndsWith(ent.Comp1.Suffix))
                    continue;

                newMarkingId = currentMarkingId[..^ent.Comp1.Suffix.Length];
            }

            if (!_prototype.HasIndex<MarkingPrototype>(newMarkingId))
            {
                Log.Warning($"{ToPrettyString(ent)} tried toggling wagging but {newMarkingId} marking doesn't exist");
                continue;
            }

            _humanoidAppearance.SetMarkingId(ent, MarkingCategories.Tail, idx, newMarkingId, false, ent.Comp2);
        }

        Dirty(ent, ent.Comp2);

        return true;
    }
}
