using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Effects;

/// <summary>
/// Splashes all of one of this entity's solutions onto the target when triggered, as a touch reaction.
/// If TargetUser is true, the user is splashed.
/// </summary>
/// <remarks>
/// Predicted, so the splash lines up with predicted triggers like slipping or alt-clicking.
/// </remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SplashOnTriggerComponent : BaseXOnTriggerComponent
{
    /// <summary>
    /// The solution of this entity to splash.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public string Solution = string.Empty;

    /// <summary>
    /// Sound played where this entity is when triggered.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? Sound;

    /// <summary>
    /// Whether this entity is deleted after splashing, for things that burst.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool DeleteAfterSplash;
}
