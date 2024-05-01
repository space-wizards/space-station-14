using Content.Shared.Inventory;
using Robust.Shared.GameStates;

namespace Content.Shared.StepTrigger.Components;

/// <summary>
/// This is used for marking step trigger events that require the user to wear shoes, such as for glass shards.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ClothingRequiredStepTriggerComponent : Component;
