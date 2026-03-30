namespace Content.Shared.Defects.Components;

/// <summary>
/// Forces the gun into semi-automatic mode at spawn by overwriting AvailableModes
/// and SelectedMode on GunComponent. Only meaningful on guns that have FullAuto.
/// </summary>
[RegisterComponent]
public sealed partial class BentSwitchDefectComponent : DefectComponent
{
    public BentSwitchDefectComponent()
    {
        Prob = 0.12f;
        DefectLabel = "bent switch";
    }
}
