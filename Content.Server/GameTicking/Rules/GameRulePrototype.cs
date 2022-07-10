using Content.Server.GameTicking.Rules.Configurations;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules;

[Prototype("gameRule")]
public sealed class GameRulePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    [DataField("config", required: true)]
    public GameRuleConfiguration Configuration { get; } = default!;
}
