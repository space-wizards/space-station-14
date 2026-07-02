using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.Antag;
using Content.Shared.Mind;
using Content.Shared.Changeling.Systems;
using Robust.Server.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Changeling.Systems;

public sealed partial class ChangelingLastResortSystem : SharedChangelingLastResortSystem
{
    private static readonly EntProtoId ChangelingRule = "Changeling";
    private static readonly ProtoId<AntagSpecifierPrototype> ChangelingAntag = "Changeling";

    [Dependency] private AntagSelectionSystem _antag = default!;
    [Dependency] private IPlayerManager _player = default!;

    protected override void TakeOverCorpse(EntityUid target, MindComponent mind)
    {
        if (mind.UserId is { } userId && _player.TryGetSessionById(userId, out var session))
        {
            _antag.TryApplyAntagConfiguration<ChangelingRuleComponent>(session,
                target,
                ChangelingRule,
                ChangelingAntag);
        }
    }
}
