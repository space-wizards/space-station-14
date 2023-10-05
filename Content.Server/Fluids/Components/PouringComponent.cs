using Robust.Shared.Audio;

namespace Content.Shared.Fluids.Components;

/// <summary>
/// </summary>
[RegisterComponent]
public sealed partial class PouringComponent : Component
{
    [DataField("manualDrainSound"), ViewVariables(VVAccess.ReadOnly)]
    public SoundSpecifier ManualDrainSound = new SoundPathSpecifier("/Audio/Effects/Fluids/slosh.ogg");
}
