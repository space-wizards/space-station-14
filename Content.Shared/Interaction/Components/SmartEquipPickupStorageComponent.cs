using Robust.Shared.GameStates;

namespace Content.Shared.Interaction.Components;

/// <summary>
/// Marks entity for <see cref="SmartEquipSystem"/> <br/>
/// Trying to smart equip this item will result in picking it up instead of taking something from <see cref="StorageComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SmartEquipPickupStorageComponent : Component;
