using Content.Shared.Ame.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Ame.Components;

/// <summary>
/// The component used to make an entity part of the bulk machinery of an AntiMatter Engine.
/// Connects to adjacent entities with this component or <see cref="AmeControllerComponent"/> to make an AME.
/// </summary>
[Access(typeof(AmeShieldingSystem), typeof(AmeNodeGroupHandler))]
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AmeShieldComponent : Component
{
    /// <summary>
    /// Whether or not this AME shield counts as a core for the AME or not.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public bool IsCore = false;

    /// <summary>
    /// The current integrity of the AME shield.
    /// </summary>
    [DataField("integrity"), AutoNetworkedField]
    public int CoreIntegrity = 100;
}
