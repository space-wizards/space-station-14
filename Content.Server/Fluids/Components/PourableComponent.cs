using Robust.Shared.Audio;

namespace Content.Shared.Fluids.Components;

/// <summary>
/// </summary>
[RegisterComponent]
public sealed partial class PourableComponent : Component
{
    [DataField("solution")]
    public string SolutionName = "tank";

    [DataField("manualDrainSound"), ViewVariables(VVAccess.ReadOnly)]
    public SoundSpecifier ManualPouringSound = new SoundPathSpecifier("/Audio/Effects/Fluids/slosh.ogg");
}
