using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.CartridgeLoader;

/// <summary>
/// This is used for defining values used for displaying in the program ui in yaml
/// </summary>
[NetworkedComponent]
[RegisterComponent]
public sealed partial class CartridgeComponent : Component
{
    [DataField("programName", required: true)]
    public string ProgramName = "default-program-name";

    [DataField("icon")]
    public SpriteSpecifier? Icon;

    public InstallationStatus InstallationStatus = InstallationStatus.Cartridge;
}

[Serializable, NetSerializable]
public sealed class CartridgeComponentState : ComponentState
{
    public InstallationStatus InstallationStatus;
}

[Serializable, NetSerializable]
public enum InstallationStatus
{
    Cartridge,
    Installed,
    Readonly
}
