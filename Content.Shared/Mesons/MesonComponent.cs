using Robust.Shared.GameStates;

namespace Content.Shared.Mesons;

[RegisterComponent, NetworkedComponent]
public sealed partial class MesonComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]

    public MesonViewType MesonType = MesonViewType.Walls;
}

public enum MesonViewType
{
    Walls,
    Radiation
}
