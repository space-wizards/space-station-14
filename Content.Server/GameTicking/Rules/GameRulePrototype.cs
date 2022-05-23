using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules;

[Prototype("gameRule")]
public sealed class GameRulePrototype : IPrototype
{
    [IdDataFieldAttribute]
    public string ID { get; } = default!;
}
