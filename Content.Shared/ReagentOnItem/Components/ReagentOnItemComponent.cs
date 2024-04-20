namespace Content.Shared.ReagentOnItem;

[ImplicitDataDefinitionForInheritors]
public abstract partial class ReagentOnItemComponent : Component
{
    [DataField]
    public double AmountOfReagentLeft;

    [DataField]
    public double ReagentCapacity = 15;
}
