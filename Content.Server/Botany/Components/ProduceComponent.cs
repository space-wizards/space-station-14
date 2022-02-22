using Content.Server.Botany.Systems;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Botany.Components;

[RegisterComponent]
[Friend(typeof(BotanySystem))]
public sealed class ProduceComponent : Component
{
    [DataField("targetSolution")] public string SolutionName { get; set; } = "food";

    [DataField("seed", required: true)] public string SeedName = default!;
}
