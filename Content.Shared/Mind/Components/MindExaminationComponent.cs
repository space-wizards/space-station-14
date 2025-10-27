using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Mind.Components;

/// <summary>
/// Types of examination messages that can be shown for different entity types.
/// </summary>
public enum MindExaminationType
{
    Humanoid,
    Silicon,
}

/// <summary>
/// Component that specifies what type of examination messages should be shown for this entity's mind state.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MindExaminationComponent : Component
{
    /// <summary>
    /// The type of examination messages to use for this entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public MindExaminationType ExaminationType = MindExaminationType.Humanoid;
}

/// <summary>
/// Extension methods to handle all ExaminationType related logic
/// </summary>
public static class MindExaminationTypeExtensions
{
    /// <summary>
    /// Gets the localization prefix for the examination type
    /// </summary>
    public static string GetLocalizationPrefix(this MindExaminationType type)
    {
        return type switch
        {
            MindExaminationType.Silicon => "comp-mind-silicon-examined",
            MindExaminationType.Humanoid => "comp-mind-humanoid-examined",
            _ => "comp-mind-humanoid-examined"
        };
    }
}
