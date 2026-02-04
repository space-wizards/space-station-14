using Robust.Shared.GameStates;

namespace Content.Shared.Traits.Assorted;

/// <summary>
/// This is used for the unrevivable trait as well as generally preventing revival.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class UnrevivableComponent : Component
{
    /// <summary>
    /// A field to define if we should display the "Genetic incompatibility" warning on health analysers
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Analyzable = true;

    /// <summary>
    /// Can this player be cloned using a cloning pod?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Cloneable = false;

    /// <summary>
    /// The loc string used to provide a reason for being unrevivable
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId ReasonMessage = "defibrillator-unrevivable";
}
