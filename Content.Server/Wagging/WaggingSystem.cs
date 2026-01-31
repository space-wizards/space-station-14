using Content.Server.Actions;
using Content.Shared.Body;
using Content.Shared.Cloning.Events;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Mobs;
using Content.Shared.Toggleable;
using Content.Shared.Wagging;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Wagging;

/// <summary>
/// Adds an action to toggle wagging animation for tails markings that supporting this
/// </summary>
public sealed class WaggingSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly SharedVisualBodySystem _visualBody = default!;
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

    private void OnCloning(Entity<WaggingComponent> ent, ref CloningEvent args)
    {
        if (!args.Settings.EventComponents.Contains(Factory.GetRegistration(ent.Comp.GetType()).Name))
            return;

        EnsureComp<WaggingComponent>(args.CloneUid);
    }

    private void OnWaggingMapInit(Entity<WaggingComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent, ref ent.Comp.ActionEntity, ent.Comp.Action, ent);
    }

    private void OnWaggingShutdown(Entity<WaggingComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.ActionEntity);
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

    private bool TryToggleWagging(Entity<WaggingComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
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

        markingsSet = markingsSet.ShallowClone();
        foreach (var (layers, markings) in markingsSet)
        {
            markingsSet[layers] = markingsSet[layers].ShallowClone();
            var layerMarkings = markingsSet[layers];

            for (int i = 0; i < layerMarkings.Count; i++)
            {
                var currentMarkingId = layerMarkings[i].MarkingId;
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
