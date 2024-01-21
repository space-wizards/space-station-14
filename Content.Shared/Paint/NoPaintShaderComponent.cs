using Robust.Shared.GameStates;

namespace Content.Shared.Paint;

/// <summary>
/// Applied to entites that should not have their layershaders altered such as Bar Signs and energy swords.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedPaintSystem))]
public sealed partial class NoPaintShaderComponent : Component
{
}
