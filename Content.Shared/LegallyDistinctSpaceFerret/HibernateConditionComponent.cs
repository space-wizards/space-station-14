namespace Content.Shared.LegallyDistinctSpaceFerret;

[RegisterComponent]
public sealed partial class HibernateConditionComponent : Component
{
    public bool Hibernated;

    [DataField]
    public string SuccessMessage = "";

    [DataField]
    public string SuccessSfx = "";
}
