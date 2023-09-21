using Content.Shared.Examine;

namespace Content.Shared.Power.Generator;

/// <summary>
/// Shared logic for power-switchable generators.
/// </summary>
/// <seealso cref="PowerSwitchableGeneratorComponent"/>
public abstract class SharedPowerSwitchableGeneratorSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<PowerSwitchableGeneratorComponent, ExaminedEvent>(GeneratorExamined);
    }

    private void GeneratorExamined(EntityUid uid, PowerSwitchableGeneratorComponent component, ExaminedEvent args)
    {
        // Show which output is currently selected.
        args.PushMarkup(Loc.GetString(
            "power-switchable-generator-examine",
            ("output", component.ActiveOutput.ToString())));
    }
}
