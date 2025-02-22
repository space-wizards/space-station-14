using Robust.Shared.Serialization;

namespace Content.Shared.Arcade.SpaceVillain.Events;

/// <summary>
///
/// </summary>
[Serializable, NetSerializable]
public sealed class SpaceVillainHealActionMessage : BoundUserInterfaceMessage;
