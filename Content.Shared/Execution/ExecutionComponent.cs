namespace Content.Shared.Execution;

[RegisterComponent]
public sealed partial class ExecutionComponent : Component
{
    [DataField]
    public float DoAfterDuration = 30f;

    [DataField]
    public float DamageModifier = 9f;

    [DataField]
    public string SuicidePopupInternal = "suicide-popup-melee-initial-internal";

    [DataField]
    public string SuicidePopupExternal = "suicide-popup-melee-initial-external";

    [DataField]
    public string ExecutionPopupInternal = "execution-popup-melee-initial-internal";

    [DataField]
    public string ExecutionPopupExternal = "execution-popup-melee-initial-external";

    [DataField]
    public string SuicidePopupCompleteInternal = "suicide-popup-melee-complete-internal";

    [DataField]
    public string SuicidePopupCompleteExternal = "suicide-popup-melee-complete-external";

    [DataField]
    public string ExecutionPopupCompleteInternal = "execution-popup-melee-complete-internal";

    [DataField]
    public string ExecutionPopupCompleteExternal = "execution-popup-melee-complete-external";
}
