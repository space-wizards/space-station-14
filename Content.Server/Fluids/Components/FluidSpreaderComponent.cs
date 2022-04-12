using Content.Server.Fluids.EntitySystems;
using Content.Shared.Chemistry.Components;

namespace Content.Server.Fluids.Components;

[RegisterComponent]
[Friend(typeof(FluidSpreaderSystem))]
public sealed class FluidSpreaderComponent : Component
{
    [ViewVariables]
    public Solution OverflownSolution = default!;

    public bool Enabled { get; set; }
}
