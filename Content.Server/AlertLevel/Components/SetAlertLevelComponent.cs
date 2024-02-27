using Content.Shared.SetAlertLevel;

namespace Content.Server.SetAlertLevel
{
    /// <summary>
    ///     Sets an alert level when activated
    /// </summary>
    [RegisterComponent]
    [Access(typeof(SetAlertLevelSystem))]
    public sealed partial class SetAlertLevelComponent : SharedSetAlertLevelComponent
    {
        [DataField("alertLevelOnActivate")] public string AlertLevelOnActivate = default!;
        [DataField("textField")] public string TextField = default!;
    }
}
