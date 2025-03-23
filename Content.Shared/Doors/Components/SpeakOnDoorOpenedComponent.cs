using Content.Shared.Dataset;
using Robust.Shared.Prototypes;

namespace Content.Shared.Doors.Components;
/// <summary>
/// Genuine People Personality.
/// Makes a door speak when being opened.
/// </summary>
/// <remarks>
[RegisterComponent]
public sealed partial class SpeakOnDoorOpenedComponent : Component
{
    /// <summary>
    /// Probability with which to speak when opened.
    /// There are a lot of doors, so we don't want this to happen every single time or chat will get spammed.
    /// </summary>
    [DataField]
    public float Probability = 0.2f;

    /// <summary>
    /// Only speak when powered?
    /// </summary>
    [DataField]
    public bool NeedsPower = true;

    /// <summary>
    /// What to say.
    /// </summary>
    [DataField]
    public ProtoId<LocalizedDatasetPrototype> Pack = "DoorGPP";
}
