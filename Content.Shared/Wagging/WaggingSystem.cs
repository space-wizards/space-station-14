using Content.Shared.Actions;
using Content.Shared.Body;
using Content.Shared.Cloning.Events;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Mobs;
using Content.Shared.Toggleable;
using JetBrains.Annotations;
using Robust.Shared.Utility;

namespace Content.Shared.Wagging;

/// <summary>
/// Adds an action to toggle the wagging animation for tails markings that support it.
/// </summary>
public sealed partial class WaggingSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedVisualBodySystem _visualBody = default!;

    /// <summary>
    /// Copies the component and its values to the clone.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnCloning(Entity<WaggingComponent> ent, ref CloningEvent args)
    {
        if (!args.Settings.EventComponents.Contains(Factory.GetRegistration(ent.Comp.GetType()).Name))
            return;

        // Make sure to set the datafields before adding the component so that the correct action gets spawned on map init.
        var cloneComp = Factory.GetComponent<WaggingComponent>();
        cloneComp.Action = ent.Comp.Action;
        cloneComp.Layer = ent.Comp.Layer;
        cloneComp.Organ = ent.Comp.Organ;
        cloneComp.Suffix = ent.Comp.Suffix;
        AddComp(args.CloneUid, cloneComp, true);
    }

    /// <summary>
    /// Adds the wagging action during initialization.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnWaggingMapInit(Entity<WaggingComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent, ref ent.Comp.ActionEntity, ent.Comp.Action, ent);
        DirtyField(ent.AsNullable(), nameof(WaggingComponent.ActionEntity));
    }

    /// <summary>
    /// Removes the wagging action during shutdown.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnWaggingShutdown(Entity<WaggingComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.ActionEntity);
    }

    /// <summary>
    /// Attempts to toggle the wagging action.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnWaggingToggle(Entity<WaggingComponent> ent, ref ToggleActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryToggleWagging(ent.AsNullable()))
            return;

        args.Handled = true;
    }

    /// <summary>
    /// Tries to disable wagging when the mob state changes.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnMobStateChanged(Entity<WaggingComponent> ent, ref MobStateChangedEvent args)
    {
        TryToggleWagging(ent.AsNullable(), false);
    }

    /// <summary>
    /// If <see cref="desired"/> provided - attempts to set wagging to it, otherwise tries to toggle wagging.
    /// </summary>
    /// <param name="ent">The entity to toggle.</param>
    /// <param name="desired">The desired wagging state.</param>
    /// <returns>Whether wagging was toggled.</returns>
    [PublicAPI]
    public bool TryToggleWagging(Entity<WaggingComponent?> ent, bool? desired = null)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (desired == ent.Comp.Wagging)
            return false;

        if (!_visualBody.TryGatherMarkingsData(ent.Owner,
                [ent.Comp.Layer],
                out _,
                out _,
                out var applied))
        {
            return false;
        }

        if (!applied.TryGetValue(ent.Comp.Organ, out var markingsSet))
            return false;

        ent.Comp.Wagging = !ent.Comp.Wagging;
        DirtyField(ent, nameof(WaggingComponent.Wagging));

        markingsSet = markingsSet.ShallowClone();
        foreach (var (layers, _) in markingsSet)
        {
            markingsSet[layers] = markingsSet[layers].ShallowClone();
            var layerMarkings = markingsSet[layers];

            for (var i = 0; i < layerMarkings.Count; i++)
            {
                var currentMarkingId = layerMarkings[i].MarkingId;
                string newMarkingId;

                if (ent.Comp.Wagging)
                {
                    newMarkingId = $"{currentMarkingId}{ent.Comp.Suffix}";
                }
                else
                {
                    if (currentMarkingId.Id.EndsWith(ent.Comp.Suffix))
                    {
                        newMarkingId = currentMarkingId.Id[..^ent.Comp.Suffix.Length];
                    }
                    else
                    {
                        newMarkingId = currentMarkingId;
                        Log.Warning($"Unable to revert wagging for {currentMarkingId}");
                    }
                }

                if (!ProtoMan.HasIndex<MarkingPrototype>(newMarkingId))
                {
                    Log.Warning($"{ToPrettyString(ent):ent} tried toggling wagging but {newMarkingId} marking doesn't exist");
                    continue;
                }

                layerMarkings[i] = new Marking(newMarkingId, layerMarkings[i].MarkingColors);
            }
        }

        _visualBody.ApplyMarkings(ent, new()
        {
            [ent.Comp.Organ] = markingsSet
        });

        return true;
    }
}
