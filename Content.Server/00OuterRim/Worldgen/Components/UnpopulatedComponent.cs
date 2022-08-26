using Content.Server._00OuterRim.Worldgen.Populators.Debris;
using Content.Server._00OuterRim.Worldgen.Systems;

namespace Content.Server._00OuterRim.Worldgen.Components;

/// <summary>
/// SHOULD NOT BE USED DIRECTLY.
/// This is an internal component used by the debris generator to lazily load debris contents.
/// It tells it which populator to use should the debris enter view range.
/// </summary>
[RegisterComponent]
[Access(typeof(PopulatorSystem), typeof(DebrisGenerationSystem))]
public sealed class UnpopulatedComponent : Component
{
    public DebrisPopulator? Populator;
}
