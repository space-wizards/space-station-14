using Robust.Shared.GameStates;

namespace Content.Shared.CartridgeLoader.Cartridges;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NotekeeperCartridgeComponent : Component
{
    /// <summary>
    /// The list of notes that got written down
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<string> Notes = new();
}
