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
    public TimeSpan DoAfterDuration = TimeSpan.FromSeconds(8);

    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");

    [DataField, AutoNetworkedField]
    public GrinderProgram Program = GrinderProgram.Grind;

    [DataField, AutoNetworkedField]
    public string SolutionName = "grinderOutput";

    [DataField, AutoNetworkedField]
    public string ItemSlotName = "grinderInput";

    [DataField, AutoNetworkedField]
    public float SolutionSize = 15f;
}
