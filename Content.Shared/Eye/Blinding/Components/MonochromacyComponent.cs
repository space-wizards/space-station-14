using Robust.Shared.GameStates;

namespace Content.Shared.Eye.Blinding.Components;

/// <summary>
/// Component used for the monochromacy status effect.
/// </summary>
[NetworkedComponent, RegisterComponent]
public sealed partial class MonochromacyComponent : Component
{
	[DataField("alpha"), ViewVariables(VVAccess.ReadWrite)]
    public float Alpha = 1.0f;
	
	[DataField("alphaglasses"), ViewVariables(VVAccess.ReadWrite)]
    public float AlphaGlasses = 0.4f;
}
