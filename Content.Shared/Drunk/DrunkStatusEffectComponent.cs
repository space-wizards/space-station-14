using Robust.Shared.GameStates;

namespace Content.Shared.Drunk;

/// <summary>
/// This is used by a status effect entity to apply the <see cref="DrunkComponent"/> to an entity.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DrunkStatusEffectComponent : Component
{
}
