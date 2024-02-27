using Content.Shared.EmergencyButton;

namespace Content.Server.EmergencyButton
{
    /// <summary>
    ///     Sets an alert level when activated
    /// </summary>
    [RegisterComponent]
    [Access(typeof(EmergencyButtonSystem))]
    public sealed partial class EmergencyButtonComponent : SharedEmergencyButtonComponent
    {
        [DataField("alertLevelOnActivate")] public string AlertLevelOnActivate = default!;
    }
}
