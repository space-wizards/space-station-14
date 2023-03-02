using Robust.Shared.Audio;

namespace Content.Server.Fluids.Components;

/// <summary>
/// Allows an entity to absorb any amount of liquid from a container (beaker etc)
/// </summary>
[RegisterComponent]
public sealed class SinkComponent : Component
{
    [DataField("emptySound")] public SoundSpecifier EmptySound = new SoundPathSpecifier("/Audio/Effects/Fluids/slosh.ogg");
}
