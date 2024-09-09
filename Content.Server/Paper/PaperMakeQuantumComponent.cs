using Content.Server.Explosion.Components;
using Content.Shared.Paper;

namespace Content.Server.Paper;

[RegisterComponent, Access(typeof(PaperMakeQuantumSystem))]
public sealed partial class PaperMakeQuantumComponent : Component
{
    [DataField]
    public float Chance = 1f;

    [DataField]
    public string? NewName;

    [DataField]
    public string? NewDesc;
    
    [DataField]
    public PaperQuantumComponent PaperQuantum = new();

    [DataField]
    public ExplosiveComponent Explosive = new();
}
