using Content.Server.Procedural.Populators.Debris;
using Content.Server.Procedural.Systems;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;

namespace Content.Server.Procedural.Components;

/// <summary>
/// SHOULD NOT BE USED DIRECTLY.
/// This is an internal component used by the debris generator to lazily load debris contents.
/// It tells it which populator to use should the debris enter view range.
/// </summary>
[RegisterComponent]
[Friend(typeof(PopulatorSystem), typeof(DebrisGenerationSystem))]
public class UnpopulatedComponent : Component
{
    public override string Name => "Unpopulated";

    public DebrisPopulator? Populator;
}
