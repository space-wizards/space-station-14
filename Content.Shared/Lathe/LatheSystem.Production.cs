using System.Linq;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Lathe.Components;
using Content.Shared.Materials;
using Content.Shared.Research.Prototypes;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Lathe;

public abstract partial class LatheSystem
{
    /// <inheritdoc cref="CanProduce(Entity{LatheComponent?},LatheRecipePrototype,int)"/>
    [PublicAPI]
    public bool CanProduce(Entity<LatheComponent?> entity, ProtoId<LatheRecipePrototype> recipe, int amount = 1)
    {
        return ProtoMan.Resolve(recipe, out var proto) && CanProduce(entity, proto, amount);
    }

    /// <summary>
    /// Checks if a lathe can produce a specific recipe prototype
    /// </summary>
    /// <param name="entity">Lathe</param>
    /// <param name="recipe">Recipe we'd like to produce</param>
    /// <param name="amount">The amount we'd like to produce</param>
    /// <returns>True if we can produce that recipe in that quantity.</returns>
    public bool CanProduce(Entity<LatheComponent?> entity, LatheRecipePrototype recipe, int amount = 1)
    {
        return CanProduce(entity, recipe, _materialStorage.GetStoredMaterials(entity.Owner), amount);
    }

    /// <inheritdoc cref="CanProduce(Entity{LatheComponent?},LatheRecipePrototype,Dictionary{ProtoId{MaterialPrototype},int},int)"/>
    [PublicAPI]
    public bool CanProduce(Entity<LatheComponent?> entity, ProtoId<LatheRecipePrototype> recipe, Dictionary<ProtoId<MaterialPrototype>, int> materials, int amount = 1)
    {
        return ProtoMan.Resolve(recipe, out var proto) && CanProduce(entity, proto, materials, amount);
    }

    /// <summary>
    /// Checks if a lathe can produce a specific recipe prototype
    /// </summary>
    /// <param name="entity">Lathe entity</param>
    /// <param name="recipe">Recipe to produce</param>
    /// <param name="materials">Materials available</param>
    /// <param name="amount">Amount to produce</param>
    /// <returns>True if we can produce the given recipe at the given amount with the given materials.</returns>
    [PublicAPI]
    public bool CanProduce(Entity<LatheComponent?> entity, LatheRecipePrototype recipe, Dictionary<ProtoId<MaterialPrototype>, int> materials, int amount = 1)
    {
        return Resolve(entity, ref entity.Comp) && HasMaterials(materials, recipe, entity.Comp.MaterialUseMultiplier, amount);
    }

    /// <inheritdoc cref="HasMaterials(Dictionary{ProtoId{MaterialPrototype},int},LatheRecipePrototype,float,int)"/>
    public bool HasMaterials(Dictionary<ProtoId<MaterialPrototype>, int> materials, ProtoId<LatheRecipePrototype> recipe, float materialMultiplier, int amount = 1)
    {
        return ProtoMan.Resolve(recipe, out var proto) && HasMaterials(materials, proto, materialMultiplier, amount);
    }

    /// <summary>
    /// Checks if a given set of materials has enough quantity of each material to produce a given amount of a recipe.
    /// </summary>
    /// <param name="materials">Materials available</param>
    /// <param name="recipe">Recipe to produce</param>
    /// <param name="materialMultiplier">Multiplier for material usage.</param>
    /// <param name="amount">Amount we'd like to produce</param>
    /// <returns>Returns true if all needed materials exist in the passed material set.</returns>
    [PublicAPI]
    public bool HasMaterials(Dictionary<ProtoId<MaterialPrototype>, int> materials, LatheRecipePrototype recipe, float materialMultiplier, int amount = 1)
    {
        foreach (var (material, needed) in recipe.Materials)
        {
            if (!materials.TryGetValue(material, out var availableAmount))
                return false;

            var adjustedAmount = AdjustMaterial(needed, recipe.ApplyMaterialDiscount, materialMultiplier);

            if (availableAmount < adjustedAmount * amount)
                return false;
        }
        return true;
    }

    /// <summary>
    /// Iterator returning adjusted amount of material needed to
    /// produce a given recipe
    /// </summary>
    private static IEnumerable<(ProtoId<MaterialPrototype> mat, int amount)> GetAdjustedAmount(Entity<LatheComponent> lathe, LatheRecipePrototype recipe)
    {
        foreach (var (mat, amount) in recipe.Materials)
        {
            var adjustedAmount = recipe.ApplyMaterialDiscount
                ? (int)(amount * lathe.Comp.MaterialUseMultiplier)
                : amount;

            yield return (mat, adjustedAmount);
        }
    }

    public bool TryAddToQueue(Entity<LatheComponent?> entity, LatheRecipePrototype recipe, int quantity)
    {
        if (!Resolve(entity, ref entity.Comp))
            return false;

        if (quantity <= 0)
            return false;

        quantity = int.Min(quantity, MaxItemsPerRequest);

        if (!CanProduce(entity, recipe, quantity))
            return false;

        foreach (var (mat, amount) in GetAdjustedAmount((entity, entity.Comp), recipe))
        {
            _materialStorage.TryChangeMaterialAmount(entity.Owner, mat, -amount * quantity);
        }

        if (entity.Comp.Queue.Last is { } node && node.ValueRef.Recipe == recipe.ID)
            node.ValueRef.ItemsRequested += quantity;
        else
            entity.Comp.Queue.AddLast(new LatheRecipeBatch(recipe.ID, 0, quantity));

        Dirty(entity);
        return true;
    }

    public bool TryStartProducing(Entity<LatheComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp))
            return false;

        return entity.Comp.CurrentRecipe == null && TryProcessQueue((entity, entity.Comp));
    }

    private bool TryProcessQueue(Entity<LatheComponent> entity)
    {
        if (!_power.IsPowered(entity.Owner) || entity.Comp.Queue.First is not { } node)
            return false;

        ref var batch = ref node;
        batch.ValueRef.ItemsPrinted++;
        if (batch.ValueRef.ItemsPrinted >= batch.ValueRef.ItemsRequested || batch.ValueRef.ItemsPrinted < 0) // Rollover sanity check
            entity.Comp.Queue.RemoveFirst();

        var recipe = ProtoMan.Index(batch.ValueRef.Recipe);

        var time = _reagentSpeed.ApplySpeed(entity.Owner, recipe.CompleteTime) * entity.Comp.TimeMultiplier;

        var lathe = EnsureComp<LatheProducingComponent>(entity);
        lathe.CompletionTime = Timing.CurTime + time;
        entity.Comp.CurrentRecipe = recipe;
        Dirty(entity, lathe);
        Dirty(entity);

        var ev = new LatheStartPrintingEvent(recipe);
        RaiseLocalEvent(entity, ref ev);

        _audio.PlayPvs(entity.Comp.ProducingSound, entity);

        if (time == TimeSpan.Zero)
        {
            FinishProducing((entity, entity.Comp));
        }

        UpdateUI((entity, entity.Comp));
        return true;
    }

    public void AbortProduction(Entity<LatheComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp))
            return;

        if (entity.Comp.CurrentRecipe != null)
        {
            if (entity.Comp.Queue.Count > 0)
            {
                // Batch abandoned while printing last item, need to create a one-item batch
                var batch = entity.Comp.Queue.First();
                if (batch.Recipe != entity.Comp.CurrentRecipe)
                {
                    var newBatch = new LatheRecipeBatch(entity.Comp.CurrentRecipe.Value, 0, 1);
                    entity.Comp.Queue.AddFirst(newBatch);
                }
                else if (batch.ItemsPrinted > 0)
                {
                    batch.ItemsPrinted--;
                }
            }

            RefundCurrentRecipe((entity, entity.Comp), entity.Comp.CurrentRecipe.Value);
            Dirty(entity);
        }

        RemComp<LatheProducingComponent>(entity);
    }

    /// <summary>
    /// Refunds the material cost of the currently running recipe,
    /// without cancelling production
    /// </summary>
    private void RefundCurrentRecipe(Entity<LatheComponent> lathe, ProtoId<LatheRecipePrototype> currentRecipe)
    {
        var recipe = ProtoMan.Index(currentRecipe);

        foreach (var (mat, amount) in GetAdjustedAmount(lathe, recipe))
        {
            _materialStorage.TryChangeMaterialAmount(lathe, mat, amount);
        }
    }

    /// <summary>
    /// Refunds the material cost of a given batch,
    /// without deleting it
    /// </summary>
    private void RefundBatch(Entity<LatheComponent> lathe, LatheRecipeBatch batch)
    {
        var delta = batch.ItemsRequested - batch.ItemsPrinted;

        var recipe = ProtoMan.Index(batch.Recipe);

        foreach (var (mat, amount) in GetAdjustedAmount(lathe, recipe))
        {
            _materialStorage.TryChangeMaterialAmount(lathe, mat, amount * delta);
        }
    }

    protected void FinishProducing(Entity<LatheComponent> entity)
    {
        if (!ProtoMan.Resolve(entity.Comp.CurrentRecipe, out var currentRecipe))
            return;

        if (currentRecipe.Result is { } resultProto)
        {
            var result = PredictedSpawnNextToOrDrop(resultProto, entity);
            _stack.TryMergeToContacts(result);
        }

        if (currentRecipe.ResultReagents is { } resultReagents &&
            entity.Comp.ReagentOutputSlotId is { } slotId)
        {
            var toAdd = new Solution(
                resultReagents.Select(p => new ReagentQuantity(p.Key.Id, p.Value)));

            // dispense it in the container if we have it and dump it if we don't
            if (_container.TryGetContainer(entity, slotId, out var container) &&
                container.ContainedEntities.Count == 1 &&
                _solution.TryGetFitsInDispenser(container.ContainedEntities.First(), out var solution, out _))
            {
                _solution.AddSolution(solution.Value, toAdd);
            }
            else
            {
                _popup.PopupEntity(Loc.GetString("lathe-reagent-dispense-no-container", ("name", entity)), entity);
                _puddle.TrySpillAt(entity, toAdd, out _);
            }
        }

        // This will dirty the component if it succeeds
        // Attempt to continue along the queue
        if (TryProcessQueue(entity))
            return;

        RemCompDeferred<LatheProducingComponent>(entity);
        Dirty(entity);
    }

}

[Serializable, NetSerializable]
public record struct LatheRecipeBatch
{
    public ProtoId<LatheRecipePrototype> Recipe;
    public int ItemsPrinted;
    public int ItemsRequested;

    public LatheRecipeBatch(ProtoId<LatheRecipePrototype> recipe, int itemsPrinted, int itemsRequested)
    {
        Recipe = recipe;
        ItemsPrinted = itemsPrinted;
        ItemsRequested = itemsRequested;
    }
}

/// <summary>
/// Event raised on a lathe when it starts producing a recipe.
/// </summary>
[ByRefEvent]
public readonly record struct LatheStartPrintingEvent(LatheRecipePrototype Recipe);
