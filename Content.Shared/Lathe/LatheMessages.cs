using Robust.Shared.Serialization;

namespace Content.Shared.Lathe;

/// <summary>
/// Sent to clients when the recipes available in the lathe have changed
/// </summary>
[Serializable, NetSerializable]
public sealed class LatheRefreshRecipesMessage : BoundUserInterfaceMessage
{
}

/// <summary>
///     Sent to the server when a client queues a new recipe.
/// </summary>
[Serializable, NetSerializable]
public sealed class LatheQueueRecipeMessage : BoundUserInterfaceMessage
{
    public readonly string ID;
    public readonly int Quantity;
    public LatheQueueRecipeMessage(string id, int quantity)
    {
        ID = id;
        Quantity = quantity;
    }
}

/// <summary>
///     Sent to the server to remove a batch from the queue.
/// </summary>
[Serializable, NetSerializable]
public sealed class LatheDeleteRequestMessage(int index) : BoundUserInterfaceMessage
{
    public int Index = index;
}

/// <summary>
///     Sent to the server to move the position of a batch in the queue.
/// </summary>
[Serializable, NetSerializable]
public sealed class LatheMoveRequestMessage(int index, int change) : BoundUserInterfaceMessage
{
    public int Index = index;
    public int Change = change;
}

/// <summary>
///     Sent to the server to stop producing the current item.
/// </summary>
[Serializable, NetSerializable]
public sealed class LatheAbortFabricationMessage() : BoundUserInterfaceMessage
{
}

[NetSerializable, Serializable]
public enum LatheUiKey
{
    Key,
}
