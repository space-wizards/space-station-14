// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Server.DeadSpace.Spiders.SpiderTerror.Components;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Server.Roles;
using Content.Server.GameTicking;
using Content.Shared.DeadSpace.Abilities.Bloodsucker;

namespace Content.Server.DeadSpace.Spiders.SpiderTerror;

public sealed class SpiderTerrorSystem : SharedBloodsuckerSystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly RoleSystem _role = default!;
    private ISawmill _logger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpiderTerrorComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<SpiderTerrorComponent, MindRemovedMessage>(OnMindRemoved);
        SubscribeLocalEvent<SpiderTerrorComponent, RoundEndTextAppendEvent>(OnRoundEnd);
    }

    private void OnMindAdded(EntityUid uid, SpiderTerrorComponent component, MindAddedMessage args)
    {
        _role.MindAddRole(args.Mind, "MindRoleSpiderTerror");

        _mind.TryAddObjective(args.Mind, args.Mind.Comp, component.Proto);
    }

    private void OnMindRemoved(EntityUid uid, SpiderTerrorComponent component, MindRemovedMessage args)
    {
        _role.MindRemoveRole<SpiderTerrorRoleComponent>((args.Mind.Owner, args.Mind.Comp));
    }

    private void OnRoundEnd(EntityUid uid, SpiderTerrorComponent component, RoundEndTextAppendEvent args)
    {
        if (!_mind.TryGetMind(uid, out var mindId, out var mindComp))
            return;

        DelObjective(mindId, mindComp, component);
    }

    private void DelObjective(EntityUid mindId, MindComponent mindComp, SpiderTerrorComponent component)
    {
        if (mindComp.Objectives == null)
        {
            _logger.Error($"Objectives is null for mindId: {mindId}");
            return;
        }

        if (_mind.TryFindObjective((mindId, mindComp), component.Proto, out var objective))
        {
            if (objective.HasValue)
            {
                var objectiveIndex = mindComp.Objectives.IndexOf(objective.Value);

                if (objectiveIndex != -1)
                {
                    _mind.TryRemoveObjective(mindId, mindComp, objectiveIndex);
                }
                else
                {
                    _logger.Warning($"Objective index not found for mindId: {mindId}, objective: {objective.Value}");
                }
            }
            else
            {
                _logger.Warning($"Objective not found for mindId: {mindId}, component: {component.Proto}");
            }
        }
        else
        {
            _logger.Warning($"Failed to find objective for mindId: {mindId}, component: {component.Proto}");
        }
    }
}
