using Content.Shared.Database;
using Content.Shared.Lathe.Components;
using Content.Shared.Materials;
using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;

namespace Content.Shared.Lathe;

public abstract partial class LatheSystem
{
    // TODO: Virtual versions of smaller UI Updates!!!
    protected virtual void UpdateUI(Entity<LatheComponent> entity) { }

    [SubscribeLocalEvent]
    private void OnLatheQueueRecipeMessage(Entity<LatheComponent> entity, ref LatheQueueRecipeMessage args)
    {
        if (ProtoMan.Resolve(args.ID, out LatheRecipePrototype? recipe))
        {
            if (TryAddToQueue(entity.AsNullable(), recipe, args.Quantity))
            {
                _adminLogger.Add(LogType.Action,
                    LogImpact.Low,
                    $"{ToPrettyString(args.Actor):player} queued {args.Quantity} {GetRecipeName(recipe)} at {ToPrettyString(entity):lathe}");
            }
        }

        TryStartProducing(entity.AsNullable());
        UpdateUI(entity);
    }

    [SubscribeLocalEvent]
    private void OnLatheSyncRequestMessage(Entity<LatheComponent> entity, ref LatheSyncRequestMessage args)
    {
        UpdateUI(entity);
    }

    /// <summary>
    /// Removes a batch from the batch queue by index.
    /// If the index given does not exist or is outside of the bounds of the lathe's batch queue, nothing happens.
    /// </summary>
    /// <param name="entity">The lathe whose queue is being altered.</param>
    /// <param name="args"></param>
    [SubscribeLocalEvent]
    public void OnLatheDeleteRequestMessage(Entity<LatheComponent> entity, ref LatheDeleteRequestMessage args)
    {
        if (args.Index < 0 || args.Index >= entity.Comp.Queue.Count)
            return;

        var node = entity.Comp.Queue.First;
        for (int i = 0; i < args.Index; i++)
        {
            node = node?.Next;
        }

        if (node == null) // Shouldn't happen with checks above.
            return;

        var batch = node.Value;
        _adminLogger.Add(LogType.Action,
            LogImpact.Low,
            $"{ToPrettyString(args.Actor):player} deleted a lathe job for ({batch.ItemsPrinted}/{batch.ItemsRequested}) {GetRecipeName(batch.Recipe)} at {ToPrettyString(entity):lathe}");

        RefundBatch(entity, batch);
        entity.Comp.Queue.Remove(node);
        Dirty(entity.AsNullable());
        UpdateUI(entity);
    }

    [SubscribeLocalEvent]
    public void OnLatheMoveRequestMessage(Entity<LatheComponent> entity, ref LatheMoveRequestMessage args)
    {
        if (args.Change == 0 || args.Index < 0 || args.Index >= entity.Comp.Queue.Count)
            return;

        // New index must be within the bounds of the batch.
        var newIndex = args.Index + args.Change;
        if (newIndex < 0 || newIndex >= entity.Comp.Queue.Count)
            return;

        var node = entity.Comp.Queue.First;
        for (int i = 0; i < args.Index; i++)
        {
            node = node?.Next;
        }

        if (node == null) // Something went wrong.
            return;

        if (args.Change > 0)
        {
            var newRelativeNode = node.Next;
            for (int i = 1; i < args.Change; i++) // 1-indexed: starting from Next
            {
                newRelativeNode = newRelativeNode?.Next;
            }

            if (newRelativeNode == null) // Something went wrong.
                return;

            entity.Comp.Queue.Remove(node);
            entity.Comp.Queue.AddAfter(newRelativeNode, node);
        }
        else
        {
            var newRelativeNode = node.Previous;
            for (int i = 1; i < -args.Change; i++) // 1-indexed: starting from Previous
            {
                newRelativeNode = newRelativeNode?.Previous;
            }

            if (newRelativeNode == null) // Something went wrong.
                return;

            entity.Comp.Queue.Remove(node);
            entity.Comp.Queue.AddBefore(newRelativeNode, node);
        }

        Dirty(entity.AsNullable());
        UpdateUI(entity);
    }

    [SubscribeLocalEvent]
    public void OnLatheAbortFabricationMessage(Entity<LatheComponent> entity, ref LatheAbortFabricationMessage args)
    {
        if (entity.Comp.CurrentRecipe is not { } recipe)
            return;

        _adminLogger.Add(LogType.Action,
            LogImpact.Low,
            $"{ToPrettyString(args.Actor):player} aborted printing {GetRecipeName(recipe)} at {ToPrettyString(entity):lathe}");

        RefundCurrentRecipe(entity, recipe);
        FinishProducing(entity);
    }

    [SubscribeLocalEvent]
    private void OnMaterialAmountChanged(Entity<LatheComponent> entity, ref MaterialAmountChangedEvent args)
    {
        UpdateUI(entity);
    }

    [SubscribeLocalEvent]
    private void OnDatabaseModified(Entity<LatheComponent> entity, ref TechnologyDatabaseModifiedEvent args)
    {
        UpdateUI(entity);
    }

    [SubscribeLocalEvent]
    private void OnResearchRegistrationChanged(Entity<LatheComponent> entity, ref ResearchRegistrationChangedEvent args)
    {
        UpdateUI(entity);
    }
}
