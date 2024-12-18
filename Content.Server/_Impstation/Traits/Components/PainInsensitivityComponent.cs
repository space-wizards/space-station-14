using Content.Shared.Mobs.Components;

namespace Content.Server._Impstation.Traits;

/// <summary>
/// When added to a mob, removes the health alert and the damage overlays
///
/// Note: This is PAINFULLY coupled to the internals of MobThresholds.
/// </summary>
[RegisterComponent, Access(typeof(PainInsensitivitySystem))]
public sealed partial class PainInsensitivityComponent : Component
{
    public bool OriginalShowOverlays;
    public bool OriginalShowBruteOverlay;
    public bool OriginalShowAirlossOverlay;
    public bool OriginalShowCritOverlay;
    public bool OriginalTriggersAlerts;

    [DataField]
    public bool ShowOverlays = true;

    [DataField]
    public bool ShowBruteOverlay = false;

    [DataField]
    public bool ShowAirlossOverlay = false;

    [DataField]
    public bool ShowCritOverlay = false;

    [DataField]
    public bool TriggersAlerts = false;

    public PainInsensitivityComponent(MobThresholdsComponent comp) : this(
        comp.ShowOverlays,
        comp.ShowBruteOverlay,
        comp.ShowAirlossOverlay,
        comp.ShowCritOverlay
    )
    {
    }

    public PainInsensitivityComponent(
        bool origShowOverlays,
        bool origShowBruteOverlay,
        bool origShowAirlossOverlay,
        bool origShowCritOverlay
    )
    {
        OriginalShowOverlays = origShowOverlays;
        OriginalShowBruteOverlay = origShowBruteOverlay;
        OriginalShowAirlossOverlay = origShowAirlossOverlay;
        OriginalShowCritOverlay = origShowCritOverlay;
    }
}
