using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry.Components.Reagents;
using Content.Shared.Chemistry.Components.Solutions;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.Systems;
using Content.Shared.Database;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Shared.Chemistry;

[UsedImplicitly]
public sealed class ReactiveSystem : EntitySystem
{
    [Robust.Shared.IoC.Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Robust.Shared.IoC.Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Robust.Shared.IoC.Dependency] private readonly SharedSolutionSystem _solutionSystem = default!;
    [Robust.Shared.IoC.Dependency] private readonly SharedChemistryRegistrySystem _chemistryRegistry = default!;

    public void DoEntityReaction(EntityUid uid, SolutionContents solutionContents, ReactionMethod method, float percentage = 1.0f)
    {
        foreach (var reagentData in solutionContents)
        {
            ReactionEntity(uid, method, reagentData, null, percentage);
        }
    }

    public void DoEntityReaction(EntityUid uid, Entity<SolutionComponent> solution, ReactionMethod method, float percentage = 1.0f)
    {
        foreach (ref var reagentData in CollectionsMarshal.AsSpan(solution.Comp.Contents))
        {
            ReactionEntity(uid, method, reagentData, solution, percentage);
        }
    }

    public void ReactionEntity(EntityUid uid,
        ReactionMethod method,
        ReagentQuantity reagentQuantity,
        Entity<SolutionComponent>? source,
        float percentage = 1.0f)
    {
        // We throw if the reagent specified doesn't exist.
        ReactionEntity(uid, method, reagentQuantity, reagentQuantity, source);
    }

    public void ReactionEntity(EntityUid uid,
        ReactionMethod method,
        Entity<ReagentDefinitionComponent> reagentDef,
        ReagentQuantity reagentQuantity,
        Entity<SolutionComponent>? source,
        float percentage = 1.0f)
    {
        if (!TryComp(uid, out ReactiveComponent? reactive))
            return;
        var quantity = percentage*reagentQuantity.Quantity;
        // If we have a source solution, use the reagent quantity we have left. Otherwise, use the reaction volume specified.
        var args = new EntityEffectReagentArgs(uid, EntityManager, null, source, quantity,
            reagentDef, method, 1f);

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
                            $"Reactive effect {effect.GetType().Name:effect} of reagent " +
                            $"{reagentDef.Comp.Id:reagent} with method {method} applied on entity " +
                            $"{ToPrettyString(entity):entity} at {Transform(entity).Coordinates:coordinates}");
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
                            $"Reactive effect {effect.GetType().Name:effect} of {ToPrettyString(entity):entity}" +
                            $" using reagent {reagentDef.Comp.Id:reagent} with method {method} at " +
                            $"{Transform(entity).Coordinates:coordinates}");
                    }

                    effect.Effect(args);
                }
            }
        }
    }

    public FixedPoint2 DoTileReaction(TileRef targetTile,
        Entity<ReagentDefinitionComponent> reagent,
        FixedPoint2 quantity)
    {
        var removed = FixedPoint2.Zero;

        if (targetTile.Tile.IsEmpty)
            return removed;

        foreach (var reaction in reagent.Comp.TileReactions)
        {
            removed += reaction.TileReact(targetTile,
                reagent,
                quantity - removed,
                EntityManager);

            if (removed > quantity)
                throw new Exception("Removed more than we have!");

            if (removed == quantity)
                break;
        }
        return removed;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FixedPoint2 DoTileReaction(TileRef targetTile, ReagentQuantity reagentQuantity)
    {
        return DoTileReaction(targetTile, reagentQuantity, reagentQuantity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DoTileReaction(TileRef targetTile,ref ReagentQuantity reagentQuantity)
    {
        var removed = DoTileReaction(targetTile, reagentQuantity, reagentQuantity.Quantity);
        reagentQuantity.Quantity -= removed;
    }

    public void DoTileReactions(TileRef targetTile, ref SolutionContents contents, float percentage = 1.0f)
    {
        percentage = Math.Clamp(percentage, 0, 1.0f);
        for (var index = 0; index < contents.Count; index++)
        {
            var reagentQuant = contents[index];
            var removed = DoTileReaction(targetTile, reagentQuant, reagentQuant.Quantity*percentage);
            contents.Remove(index,removed);
        }
    }

    public SolutionContents DoTileReactions(TileRef targetTile, SolutionContents contents, float percentage = 1.0f)
    {
        percentage = Math.Clamp(percentage, 0, 1.0f);
        for (var index = 0; index < contents.Count; index++)
        {
            var reagentQuant = contents[index];
            var removed = DoTileReaction(targetTile, reagentQuant, reagentQuant.Quantity*percentage);
            contents.Remove(index,removed);
        }
        return contents;
    }
}
public enum ReactionMethod
{
Touch,
Injection,
Ingestion,
}
