using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.GameTicking.Rules;

[Prototype("gameRule")]
public sealed class GameRulePrototype : IPrototype
{
    [IdDataFieldAttribute]
    public string ID { get; } = default!;
}
