using Content.Shared.Examine;

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
