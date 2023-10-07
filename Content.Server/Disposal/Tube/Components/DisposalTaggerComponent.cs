using Content.Server.Disposal.Unit.Components;
using Content.Server.UserInterface;
using Robust.Shared.Audio;

namespace Content.Server.Disposal.Tube.Components;

/// <summary>
/// Adds a tag to contents that pass through this pipe.
/// </summary>
[RegisterComponent]
public sealed partial class DisposalTaggerComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string Tag = string.Empty;

    [DataField]
    public SoundSpecifier ClickSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg")
    {
        Params = AudioParams.Default.WithVolume(-2f)
    };
}
