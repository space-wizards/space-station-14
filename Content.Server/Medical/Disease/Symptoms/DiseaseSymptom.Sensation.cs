using Content.Shared.Popups;
using Content.Shared.Medical.Disease;
using Content.Server.Popups;

namespace Content.Server.Medical.Disease.Symptoms;

[DataDefinition]
public sealed partial class SymptomSensation : SymptomBehavior
{
    /// <summary>
    /// Localization key for the popup text.
    /// </summary>
    [DataField(required: true)]
    public string Popup { get; private set; } = string.Empty;

    /// <summary>
    /// Popup visual style.
    /// </summary>
    [DataField]
    public PopupType PopupType { get; private set; } = PopupType.Small;
}

public sealed partial class SymptomSensation
{
    [Dependency] private readonly PopupSystem _popup = default!;

    /// <summary>
    /// Shows a small popup to the carrier with the configured localization key.
    /// </summary>
    public override void OnSymptom(EntityUid uid, DiseasePrototype disease)
    {
        var text = Loc.GetString(Popup);
        if (string.IsNullOrEmpty(text))
            return;

        _popup.PopupEntity(text, uid, uid, PopupType);
    }
}
