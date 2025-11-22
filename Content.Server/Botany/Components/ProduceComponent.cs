using Content.Server.Botany.Systems;
using Content.Shared.Botany.Components;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Botany.Components;

[RegisterComponent]
[Access(typeof(BotanySystem))]
public sealed partial class ProduceComponent : SharedProduceComponent
{
    /// <summary>
    /// Name of the solution container that holds the produce's contents.
    /// </summary>
    [DataField("targetSolution")] public string SolutionName { get; set; } = "food";

    /// <summary>
    /// Seed data used to create a <see cref="SeedComponent"/> when this produce has its seeds extracted.
    /// </summary>
    [DataField]
    public SeedData? Seed;

    /// <summary>
    /// Prototype ID for the seed that can be extracted from this produce.
    /// </summary>
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<SeedPrototype>))]
    public string? SeedId;
}
