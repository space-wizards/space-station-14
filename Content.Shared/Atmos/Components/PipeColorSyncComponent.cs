using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Serilog;

namespace Content.Shared.Atmos.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class PipeColorSyncComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public Color SyncColor = new Color(255, 255, 255);
}
