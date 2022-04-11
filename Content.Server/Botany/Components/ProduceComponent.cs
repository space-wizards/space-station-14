using Content.Server.Botany.Systems;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Botany.Components;

[RegisterComponent]
[Friend(typeof(BotanySystem))]
public sealed class ProduceComponent : Component
{
    [DataField("targetSolution")] public string SolutionName { get; set; } = "food";

    /// <summary>
    ///     Name of a base seed prototype that this produce can spawn.
    /// </summary>
    [DataField("seed", customTypeSerializer: typeof(PrototypeIdSerializer<SeedPrototype>))]
    public readonly string? SeedName;

    /// <summary>
    ///     Uid of a modified seed prototype that this produce can spawn. Takes priority over <see cref="SeedName"/>.
    /// </summary>
    [DataField("SeedUid")]
    public int? SeedUid;
}
