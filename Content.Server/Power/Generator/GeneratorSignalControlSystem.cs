using System.ComponentModel;
using Content.Server.DeviceLinking.Events;
using Content.Shared.Power.Generator;

namespace Content.Server.Power.Generator;

public sealed class GeneratorSignalControlSystem: EntitySystem
{
    [Dependency] private readonly GeneratorSystem _generator = default!;
    [Dependency] private readonly ActiveGeneratorRevvingSystem _revving = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GeneratorSignalControlComponent, SignalReceivedEvent>(OnSignalReceived);
    }

    /// <summary>
    /// Change the state of the generator depending on what signal is sent.
    /// </summary>
    private void OnSignalReceived(EntityUid uid, GeneratorSignalControlComponent component, SignalReceivedEvent args)
    {
        if (!TryComp<FuelGeneratorComponent>(uid, out var generator))
            return;

        if (args.Port == component.OnPort)
        {
            _revving.StartAutoRevving(uid);
        }
        else if (args.Port == component.OffPort)
        {
            _generator.SetFuelGeneratorOn(uid, false, generator);
            _revving.StopAutoRevving(uid);
        }
        else if (args.Port == component.TogglePort)
        {
            if (generator.On)
            {
                _generator.SetFuelGeneratorOn(uid, false, generator);
                _revving.StopAutoRevving(uid);
            }
            else
            {
                _revving.StartAutoRevving(uid);
            }
        }
    }
}
