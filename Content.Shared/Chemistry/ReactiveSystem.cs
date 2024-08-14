using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.Reagents;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.Systems;
using Content.Shared.Database;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.Chemistry;

[UsedImplicitly]
public sealed class ReactiveSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedChemistryRegistrySystem _chemistryRegistry = default!;

    public void DoEntityReaction(EntityUid uid, Solution solution, ReactionMethod method)
    {
        foreach (var reagent in solution.Contents.ToArray())
        {
            ReactionEntity(uid, method, reagent, solution);
        }
    }

    public void ReactionEntity(EntityUid uid, ReactionMethod method, ReagentQuantity reagentQuantity, Solution? source)
    {
        // We throw if the reagent specified doesn't exist.
        ReactionEntity(uid, method, reagentQuantity, reagentQuantity, source);
    }

    public void ReactionEntity(EntityUid uid, ReactionMethod method, Entity<ReagentDefinitionComponent> reagentDef,
        ReagentQuantity reagentQuantity, Solution? source)
    {
        if (!TryComp(uid, out ReactiveComponent? reactive))
            return;

        // If we have a source solution, use the reagent quantity we have left. Otherwise, use the reaction volume specified.
        var args = new EntityEffectReagentArgs(uid, EntityManager, null, source, source?.GetReagentQuantity(reagentQuantity.Reagent) ?? reagentQuantity.Quantity, reagentDef, method, 1f);

        // First, check if the reagent wants to apply any effects.
        if (reagentDef.Comp.ReactiveEffects != null && reactive.ReactiveGroups != null)
        {
            foreach (var (key, val) in reagentDef.Comp.ReactiveEffects)
            {
                if (!val.Methods.Contains(method))
                    continue;

                if (!reactive.ReactiveGroups.ContainsKey(key))
                    continue;

                if (!reactive.ReactiveGroups[key].Contains(method))
                    continue;

                foreach (var effect in val.Effects)
                {
                    if (!effect.ShouldApply(args, _robustRandom))
                        continue;

                    if (effect.ShouldLog)
                    {
                        var entity = args.TargetEntity;
                        _adminLogger.Add(LogType.ReagentEffect, effect.LogImpact,
                            $"Reactive effect {effect.GetType().Name:effect} of reagent {reagentDef.Comp.Id:reagent} with method {method} applied on entity {ToPrettyString(entity):entity} at {Transform(entity).Coordinates:coordinates}");
                    }

                    effect.Effect(args);
                }
            }
        }

        // Then, check if the prototype has any effects it can apply as well.
        if (reactive.Reactions != null)
        {
            foreach (var entry in reactive.Reactions)
            {
                if (!entry.Methods.Contains(method))
                    continue;

                if (entry.Reagents != null && !entry.Reagents.Contains(reagentDef.Comp.Id))
                    continue;

                foreach (var effect in entry.Effects)
                {
                    if (!effect.ShouldApply(args, _robustRandom))
                        continue;

                    if (effect.ShouldLog)
                    {
                        var entity = args.TargetEntity;
                        _adminLogger.Add(LogType.ReagentEffect, effect.LogImpact,
                            $"Reactive effect {effect.GetType().Name:effect} of {ToPrettyString(entity):entity} using reagent {reagentDef.Comp.Id:reagent} with method {method} at {Transform(entity).Coordinates:coordinates}");
                    }

                    effect.Effect(args);
                }
            }
        }
    }

    public FixedPoint2 ReactionTile(TileRef tile, Entity<ReagentDefinitionComponent> reagentDef, FixedPoint2 reactVolume, IEntityManager entityManager)
    {
        var removed = FixedPoint2.Zero;

        if (tile.Tile.IsEmpty)
            return removed;

        foreach (var reaction in reagentDef.Comp.TileReactions)
        {
            removed += reaction.TileReact(tile, reagentDef, reactVolume - removed, entityManager);

            if (removed > reactVolume)
                throw new Exception("Removed more than we have!");

            if (removed == reactVolume)
                break;
        }

        return removed;
    }
}
public enum ReactionMethod
{
Touch,
Injection,
Ingestion,
}
