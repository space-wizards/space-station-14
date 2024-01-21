namespace Content.Shared.Execution;

[RegisterComponent]
public sealed partial class ExecutionComponent : Component
{
    [DataField]
    public float DoAfterDuration = 5f;

    [DataField]
    public float DamageModifier = 9f;

    [DataField]
    public string? SuicidePopupInternal;

    [DataField]
    public string? SuicidePopupExternal;

    [DataField]
    public string? ExecutionPopupInternal;

    [DataField]
    public string? ExecutionPopupExternal;

    [DataField]
    public string? SuicidePopupCompleteInternal;

    [DataField]
    public string? SuicidePopupCompleteExternal;

    [DataField]
    public string? ExecutionPopupCompleteInternal;

    [DataField]
    public string? ExecutionPopupCompleteExternal;

    [DataField]
    public string FixtureId = "projectile";

}
