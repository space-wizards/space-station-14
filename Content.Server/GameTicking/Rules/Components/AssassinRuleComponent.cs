using Content.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Server.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(Content.Server.GameTicking.Rules.AssassinRuleSystem))]
public sealed partial class AssassinRuleComponent : Component
{
    /// <summary>
    /// How often (roughly) we should check for objective completion to end the round.
    /// </summary>
    [DataField("endCheckDelay")] public TimeSpan EndCheckDelay = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Next time (game timing) to perform the round-end check.
    /// </summary>
    [ViewVariables] public TimeSpan? NextRoundEndCheck = null;
}
