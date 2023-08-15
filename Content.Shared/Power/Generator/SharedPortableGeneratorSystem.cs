using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Robust.Shared.Serialization;

namespace Content.Shared.Power.Generator;

/// <summary>
/// Shared logic for portable generators.
/// </summary>
/// <seealso cref="PortableGeneratorComponent"/>
public abstract class SharedPortableGeneratorSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<PortableGeneratorComponent, ExaminedEvent>(GeneratorExamined);
    }

    private void GeneratorExamined(EntityUid uid, PortableGeneratorComponent component, ExaminedEvent args)
    {
        // Show which output is currently selected.
        args.PushMarkup(Loc.GetString("portable-generator-examine", ("output", component.ActiveOutput.ToString())));
    }
}

/// <summary>
/// Used to start a portable generator.
/// </summary>
/// <seealso cref="SharedPortableGeneratorSystem"/>
[Serializable, NetSerializable]
public sealed class GeneratorStartedEvent : DoAfterEvent
{
    public override DoAfterEvent Clone()
    {
        return this;
    }
}
