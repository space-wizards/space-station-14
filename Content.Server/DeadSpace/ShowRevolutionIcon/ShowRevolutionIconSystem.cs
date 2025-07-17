// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Server.GameTicking.Rules.Components;
using Content.Shared.DeadSpace.ShowRevolutionIcon;
using Content.Shared.Ghost;
using Robust.Shared.Player;

namespace Content.Server.DeadSpace.ShowRevolutionIcon;

/// <summary>
/// This handles makes it possible to see the revolutionaries icon at the second stage
/// </summary>
public sealed class ShowRevolutionIconSystem : EntitySystem
{
    private bool _isMassacre = false;

    /// <inheritdoc/>

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<RevolutionaryRuleComponent>();
        var queryActor = EntityQueryEnumerator<ActorComponent>();

        while (query.MoveNext(out var _, out var comp))
            if (comp.Stage == RevolutionaryStage.Massacre)
            {
                _isMassacre = true;
            }

        if (!_isMassacre)
            return;

        while (queryActor.MoveNext(out var uid, out var _))
        {
            if (HasComp<GhostComponent>(uid))
                continue;

            var showComp = EnsureComp<ShowRevolutionIconComponent>(uid);
            Dirty(uid, showComp);
        }
    }
}
