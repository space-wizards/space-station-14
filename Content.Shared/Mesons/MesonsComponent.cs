namespace Content.Shared.Mesons;

[RegisterComponent]
public sealed partial class MesonsComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public MesonsViewType MesonsType = MesonsViewType.Walls;
}

public enum MesonsViewType
{
    Walls,
    Radiation
}
