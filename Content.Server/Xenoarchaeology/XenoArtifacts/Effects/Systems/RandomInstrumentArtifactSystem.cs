using Content.Server.Instruments;
using Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;
using Content.Shared.Instruments;
using Robust.Shared.Random;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Systems;

public sealed class RandomInstrumentArtifactSystem : EntitySystem
{
    [Dependency] private readonly InstrumentSystem _instrument = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<RandomInstrumentArtifactComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(EntityUid uid, RandomInstrumentArtifactComponent component, ComponentStartup args)
    {
        var instrument = EnsureComp<InstrumentComponent>(uid);
        _instrument.SetInstrumentProgram(uid, instrument, (byte) _random.Next(0, 127), 0);
    }
}
