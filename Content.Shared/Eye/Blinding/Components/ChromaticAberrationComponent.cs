using Robust.Shared.GameStates;

namespace Content.Shared.Eye.Blinding.Components;

/// <summary>
/// Component used for the Chromatic Aberration status effect.
/// </summary>
[NetworkedComponent, RegisterComponent, AutoGenerateComponentState]
public sealed partial class ChromaticAberrationComponent : Component
{
	[DataField("alpha"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float Alpha = 1.0f;
	
	[DataField("alphaglasses"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float AlphaGlasses = 0.4f;
	
	[DataField("setPreset"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public string? SetPreset;
	
	// Color Matrix Mess. God save us all.
	[DataField("a1"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float A1 = 0.625f;
	[DataField("a2"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float A2 = 0.375f;
	[DataField("a3"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float A3 = 0f;
	
	[DataField("b1"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float B1 = 0.7f;
	[DataField("b2"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float B2 = 0.3f;
	[DataField("b3"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float B3 = 0f;
	
	[DataField("c1"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float C1 = 0f;
	[DataField("c2"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float C2 = 0.3f;
	[DataField("c3"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float C3 = 0.7f;
	
}
