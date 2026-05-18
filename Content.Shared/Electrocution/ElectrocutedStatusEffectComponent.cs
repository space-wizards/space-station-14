using Robust.Shared.GameStates;

namespace Content.Shared.Electrocution;

/// <summary>
/// Electrocution as a status effect.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedElectrocutionSystem))]
public sealed partial class ElectrocutedStatusEffectComponent : Component;
