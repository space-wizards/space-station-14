
namespace Content.Server.DeadSpace.Drug.Components;

[RegisterComponent]
public sealed partial class DrugTestStickComponent : Component
{
    [DataField("dna"), ViewVariables(VVAccess.ReadOnly)]
    public string DNA = String.Empty;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int DependencyLevel = 0;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float AddictionLevel = 0;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Tolerance = 0;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float WithdrawalLevel = 0;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ThresholdTime = 0;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public bool IsUsed = false;

}
