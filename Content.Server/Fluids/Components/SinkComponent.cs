using Content.Server.Fluids.EntitySystems;
using Robust.Shared.Audio;
namespace Content.Server.Fluids.Components;

/// <summary>
/// Allows an entity to absorb any amount of liquid from a container (beaker etc)
/// </summary>
[RegisterComponent, Access(typeof(SpillableSystem))]
public sealed class SinkComponent : Component
{
    [DataField("emptySound"), ViewVariables(VVAccess.ReadOnly)]
    public SoundSpecifier EmptySound = new SoundPathSpecifier("/Audio/Effects/Fluids/slosh.ogg");
}
