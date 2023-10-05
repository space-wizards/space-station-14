using Content.Server.Fluids.EntitySystems;
using Robust.Shared.Audio;

namespace Content.Shared.Fluids.Components;

/// <summary>
/// Adds a "Pour Liquid" option to the context menu.
/// This allows both filling a bucket from this container and topping it up with any liquid.
/// </summary>
[RegisterComponent, Access(typeof(PourableSystem))]
public sealed partial class PourableComponent : Component
{
    [DataField("solution")]
    public string SolutionName = "tank";

    [DataField("manualDrainSound"), ViewVariables(VVAccess.ReadOnly)]
    public SoundSpecifier ManualPouringSound = new SoundPathSpecifier("/Audio/Effects/Fluids/slosh.ogg");
}
