namespace Content.Shared.ReagentOnItem;

[ImplicitDataDefinitionForInheritors]
public abstract partial class ReagentOnItemComponent : Component
{
    [DataField("amountOfReagentLeft"), ViewVariables(VVAccess.ReadWrite)]
    public double AmountOfReagentLeft;

    [DataField("reagentCapacity")]
    public double ReagentCapacity = 15;
}
