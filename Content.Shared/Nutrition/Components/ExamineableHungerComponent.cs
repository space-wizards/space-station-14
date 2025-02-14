using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Nutrition.Components;

/// <summary>
/// Adds text to the entity's description box based on its current hunger threshold.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ExamineableHungerSystem))]
public sealed partial class ExamineableHungerComponent : Component
{
    /// <summary>
    /// Dictionary of hunger thresholds to LocIds of the messages to display.
    /// </summary>
    [DataField]
    public Dictionary<HungerThreshold, LocId> Descriptions = new()
    {
        { HungerThreshold.Overfed, "examineable-hunger-component-examine-overfed"},
        { HungerThreshold.Okay, "examineable-hunger-component-examine-okay"},
        { HungerThreshold.Peckish, "examineable-hunger-component-examine-peckish"},
        { HungerThreshold.Starving, "examineable-hunger-component-examine-starving"},
        { HungerThreshold.Dead, "examineable-hunger-component-examine-starving"}
    };

    /// <summary>
    /// LocId of a fallback message to display if the entity has no <see cref="HungerComponent"/>
    /// or does not have a value in <see cref="Descriptions"/> for the current threshold.
    /// </summary>
    public LocId NoHungerDescription = "examineable-hunger-component-examine-none";
}
