using Microsoft.CodeAnalysis;
using Robust.Shared.GameObjects;

namespace Content.Server.Fluids.Components;

[RegisterComponent]
public  class FluidSpreaderComponent : Component
{
    public Solution OverflownSolution = default!;

    public bool Enabled { get; set; }
}