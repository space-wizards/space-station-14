namespace Content.Shared.Traits.Assorted;

/// <summary>
/// This is used for the unrevivable trait.
/// </summary>
[RegisterComponent]
public sealed partial class UnrevivableComponent : Component
{
    /// <summary>
    /// A field to define if we should display the "Genetic incompatibility" warning on health analysers
    /// </summary>
    [DataField]
    public bool Analyzable { get; set; } = true;

    /// <summary>
    /// The loc string used to provide a reason for being unrevivable
    /// </summary>
    public string ReasonMessage = "changeling-defibrillator-failure";
}
