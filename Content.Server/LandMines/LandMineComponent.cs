namespace Content.Server.LandMines;

[RegisterComponent]
public sealed partial class LandMineComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool ExplodeImmediately = false;
}
