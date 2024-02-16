using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Power.Generator;

/// <summary>
/// Shared logic for portable generators.
/// </summary>
/// <seealso cref="PortableGeneratorComponent"/>
public abstract class SharedPortableGeneratorSystem : EntitySystem
{
}

/// <summary>
/// Used to start a portable generator.
/// </summary>
/// <seealso cref="SharedPortableGeneratorSystem"/>
[Serializable, NetSerializable]
public sealed partial class GeneratorStartedEvent : DoAfterEvent
{
    public override DoAfterEvent Clone()
    {
        return this;
    }
}

/// <summary>
/// Used to start a portable generator. This is like <see cref="GeneratorStartedEvent"/> except it isn't a do-after.
/// </summary>
[ByRefEvent]
public sealed partial class AutoGeneratorStartedEvent
{
    public bool Started = false;
}
