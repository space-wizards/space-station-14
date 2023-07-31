namespace Content.Server.Flash.Components;

[RegisterComponent, Access(typeof(FlashSystem))]

public sealed class RevolutionaryConversionFlashComponent
{
    [DataField("stunTime")]
    [ViewVariables(VVAccess.ReadWrite)]
    public int StunTime = 4;
}
