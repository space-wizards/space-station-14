using Content.Shared.Chemistry.Components;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Kitchen.Components;

/// <summary>
/// Indicates this entity is a handheld grinder.
/// Entities with <see cref="ExtractableComponent"/> can be used on handheld grinders to extract their solutions.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HandheldGrinderComponent : Component
{
    /// <summary>
    /// The length of the doAfter.
    /// After it ends, the respective GrinderProgram is used on the contents.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan DoAfterDuration = TimeSpan.FromSeconds(4f);

    /// <summary>
    /// Popup to use after the current item is done processing.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId FinishedPopup = "handheld-grinder-default";

    /// <summary>
    /// The sound to play when the doAfter starts.
    /// </summary>
    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Items/Culinary/mortar_grinding.ogg", AudioParams.Default.WithLoop(true));

    /// <summary>
    /// The grinder program to use.
    /// Decides whether this one will Juice or Grind the objects.
    /// </summary>
    [DataField, AutoNetworkedField]
    public GrinderProgram Program = GrinderProgram.Grind;

    /// <summary>
    /// The solution into which the output reagents will go.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string SolutionName = "grinderOutput";

    /// <summary>
    /// Cached solution from the grinder.
    /// </summary>
    [ViewVariables]
    public Entity<SolutionComponent>? GrinderSolution;

    // Used to cancel the sound.
    public EntityUid? AudioStream;
}
