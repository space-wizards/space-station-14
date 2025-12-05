using Content.Shared.Actions;
using Content.Shared.Cloning.Events;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Mobs;
using Content.Shared.Toggleable;
using Robust.Shared.Prototypes;

namespace Content.Shared.Wagging;

/// <summary>
/// Adds an action to toggle wagging animation for tails markings that supporting this.
/// </summary>
public sealed class WaggingSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoidAppearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WaggingComponent, MapInitEvent>(OnWaggingMapInit);
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

    private void OnWaggingMapInit(Entity<WaggingComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent.Owner, ref ent.Comp.ActionEntity, ent.Comp.Action, ent.Owner);
        Dirty(ent);
    }

    private void OnWaggingShutdown(Entity<WaggingComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.ActionEntity);
        Dirty(ent);
    }

    private void OnWaggingToggle(Entity<WaggingComponent> ent, ref ToggleActionEvent args)
    {
        if (args.Handled)
            return;

        TryToggleWagging(ent.AsNullable());
    }

    private void OnMobStateChanged(Entity<WaggingComponent> ent, ref MobStateChangedEvent args)
    {
        if (ent.Comp.Wagging)
            TryToggleWagging(ent.AsNullable());
    }

    /// <summary>
    /// Toggles the wagging animation state for all tail markings on a humanoid entity.
    /// </summary>
    /// <param name="ent">The entity owning the <see cref="WaggingComponent"/>.</param>
    /// <param name="humanoid">The humanoid's appearance component.</param>
    /// <returns>
    public bool TryToggleWagging(Entity<WaggingComponent?> ent, HumanoidAppearanceComponent? humanoid = null)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, ref humanoid))
            return false;

        if (!humanoid.MarkingSet.Markings.TryGetValue(MarkingCategories.Tail, out var markings))
            return false;

        if (markings.Count == 0)
            return false;

        ent.Comp.Wagging = !ent.Comp.Wagging;
        Dirty(ent);

        for (var idx = 0; idx < markings.Count; idx++) // Animate all possible tails.
        {
            var currentMarkingId = markings[idx].MarkingId;
            string newMarkingId;

            if (ent.Comp.Wagging)
            {
                newMarkingId = $"{currentMarkingId}{ent.Comp.Suffix}";
            }
            else
            {
                if (currentMarkingId.EndsWith(ent.Comp.Suffix))
                {
                    newMarkingId = currentMarkingId[..^ent.Comp.Suffix.Length];
                }
                else
                {
                    newMarkingId = currentMarkingId;
                    Log.Warning($"Unable to revert wagging for {currentMarkingId}");
                }
            }

            if (!_prototype.HasIndex<MarkingPrototype>(newMarkingId))
            {
                Log.Warning($"{ToPrettyString(ent.Owner)} tried toggling wagging but {newMarkingId} marking doesn't exist");
                continue;
            }

            _humanoidAppearance.SetMarkingId((ent.Owner, humanoid), MarkingCategories.Tail, idx, newMarkingId);
        }

        return true;
    }
}
