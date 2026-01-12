using Content.Shared.Chemistry.Components;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Kitchen.Components;

/// <summary>
/// Indicates this entity is a handheld grinder.
/// Handheld grinders can be used with entities with <see cref="ExtractableComponent"/> to extract their solutions.
/// Requires <see cref="ItemSlotsComponent"/>
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
    /// The item slot into which the input items will go.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string ItemSlotName = "grinderInput";

    /// <summary>
    /// Cached solution from the grinder.
    /// </summary>
    [ViewVariables]
    public Entity<SolutionComponent>? GrinderSolution;

    // Used to cancel the sound.
    public EntityUid? AudioStream;
}
