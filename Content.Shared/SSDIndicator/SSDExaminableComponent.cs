
using Robust.Shared.GameStates;

namespace Content.Shared.SSDIndicator;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class SSDExaminableComponent : Component
{
    /// <summary>
    ///     Whether examining should show information about the mind or not.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ShowExamineInfo = true;

    [DataField, AutoNetworkedField]
    public SSDStatus Status = SSDStatus.Normal;
}

public enum SSDStatus : byte
{
    Normal,
    SSD,
    Catatonic
}
