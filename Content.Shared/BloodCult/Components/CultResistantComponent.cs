using Robust.Shared.GameStates;

namespace Content.Shared.BloodCult.Components;

/// <summary>
/// Given to chaplains at round start to protect them from the effects of the Blood Cult spells and daggers
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CultResistantComponent : Component;
