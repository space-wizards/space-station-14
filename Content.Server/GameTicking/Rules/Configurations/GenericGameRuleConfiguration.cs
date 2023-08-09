using JetBrains.Annotations;

namespace Content.Server.GameTicking.Rules.Configurations;

/// <summary>
/// A generic configuration, for game rules that don't have special config data.
/// </summary>
[UsedImplicitly]
public sealed class GenericGameRuleConfiguration : GameRuleConfiguration
{
    [DataField("id", required: true)]
    private string _id = default!;
    public override string Id => _id;
}
