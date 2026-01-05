using Robust.Shared.GameStates;

namespace Content.Shared.Interaction.Components;

/// <summary>
///     Used for entities that we would rather not take the contents of when using smart equip
///     Like guns.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class IgnoreContentsOnSmartEquipComponent : Component;
