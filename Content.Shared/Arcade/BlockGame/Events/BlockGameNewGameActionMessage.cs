using Robust.Shared.Serialization;

namespace Content.Shared.Arcade.BlockGame.Events;

/// <summary>
///
/// </summary>
[Serializable, NetSerializable]
public sealed class BlockGameNewGameActionMessage : BoundUserInterfaceMessage;
