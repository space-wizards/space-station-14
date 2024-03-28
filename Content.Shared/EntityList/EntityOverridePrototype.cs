using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.EntityList;

/// <summary>
/// Defines a preset to govern the behavior of <see cref="EntityOverrideSystem"/>.
/// </summary>
[Prototype("entityOverride")]
public sealed partial class EntityOverridePrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public ProtoId<SpeciesPrototype>? Species { get; private set; } = null;

    [DataField]
    public ProtoId<JobPrototype>? Job { get; private set; } = null;

    [DataField]
    public int MaxReplacements { get; private set; } = int.MaxValue;

    [DataField(required: true)]
    public ProtoId<EntityPrototype> Target { get; private set; }

    // Also requires at least one of the following...
    [DataField]
    private string? Single { get; set; } = null;

    [DataField]
    private List<string>? Random { get; set; } = null;

    [DataField]
    private Dictionary<string, string>? Keyed { get; set; } = null;

    public string Pick(IRobustRandom random, string? key = null)
    {
        if (Single != null)
            return Single;
        else if (Random != null)
            return Random[random.Next(Random.Count)];
        else if (Keyed != null && key != null)
            return Keyed.GetValueOrDefault(key, "");
        else
            return "";
    }
}
