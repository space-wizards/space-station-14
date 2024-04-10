namespace Content.Shared.ReagentOnItem;

[ImplicitDataDefinitionForInheritors]
public abstract partial class ReagentOnItemComponent : Component
{
    [DataField("amountOfReagentLeft"), ViewVariables(VVAccess.ReadWrite)]
    public Double AmountOfReagentLeft;

    [DataField("reagentCapacity")]
    public Double ReagentCapacity = 15;
}
