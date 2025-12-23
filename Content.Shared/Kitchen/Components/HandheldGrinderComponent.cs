using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Kitchen.Components;

/// <summary>
/// Indicates this entity is a handheld grinder.
/// Using an entity that can be ground or juiced on this allows to extract the solution
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HandheldGrinderComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan DoAfterDuration = TimeSpan.FromSeconds(3.5f);

    /// <summary>
    /// The sound to play when the doAfter starts.
    /// </summary>
    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Items/Culinary/mortar_grinding.ogg");

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

    // Used to cancel the sound.
    public EntityUid? AudioStream;
}
