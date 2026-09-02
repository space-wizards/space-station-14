namespace Content.Shared.Speech.Components;

/// <summary>
/// Base class for accent components.
/// </summary>
public abstract partial class BaseAccentComponent : Component
{
    /// <summary>
    /// Allow the accent to be relayed through inventory, e.g. for clothing.
    /// </summary>
    [DataField]
    public virtual bool RelayAccent { get; set; }
}
