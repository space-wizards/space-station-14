namespace Content.Server._Impstation.CosmicCult.Components;

[RegisterComponent]
[AutoGenerateComponentPause]
public sealed partial class CosmicAstralBodyComponent : Component
{
    [ViewVariables]
    [AutoPausedField]
    public TimeSpan EndAstralTime = default!;

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid OriginalBody;
}
