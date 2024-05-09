using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Mesons;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MesonsComponent : Component
{
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId Action = "ActionToggleMesons";

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? ActionEntity;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public bool Enabled;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public MesonsViewType MesonsType = MesonsViewType.Walls;
}

public enum MesonsViewType
{
    Walls,
    Radiation
}
