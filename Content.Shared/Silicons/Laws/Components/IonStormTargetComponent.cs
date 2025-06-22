namespace Content.Shared.Silicons.Laws.Components;

/// <summary>
/// Component that designates an entity to be affected by ion storms.
/// During the ion storm event, this entity will have <see cref="IonStormEvent"/> raised on it, in addition to <see cref="IonStormLawsEvent"/>  if it has laws.
/// New laws can be modified in multiple ways depending on the fields in <see cref="SiliconLawBoundComponent"/> .
/// </summary>
[RegisterComponent]
public sealed partial class IonStormTargetComponent : Component
{
    /// <summary>
    /// Chance for this entity to be affected at all.
    /// </summary>
    [DataField]
    public float Chance = 0.8f;
}

/// <summary>
/// Event raised on an entity with <see cref="IonStormTargetComponent"/> when an ion storm occurs on the attached station.
/// </summary>
[ByRefEvent]
public record struct IonStormEvent(bool Adminlog = true);
