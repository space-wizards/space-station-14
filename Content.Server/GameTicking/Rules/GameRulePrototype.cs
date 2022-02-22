using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.GameTicking.Rules;

[Prototype("gameRule")]
public sealed class GameRulePrototype : IPrototype
{
    [DataField("id", required:true)]
    public string ID { get; } = default!;
}
