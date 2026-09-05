using Content.Shared.Antag;
using Content.Shared.GameTicking.Rules.Components;
using Content.Shared.Localizations;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Content.Shared.Station.Systems;

namespace Content.Shared.GameTicking.Rules;

public sealed partial class DragonRuleSystem : GameRuleSystem<DragonRuleComponent>
{
    [Dependency] private AntagSelectionSystem _antag = default!;
    [Dependency] private StationSystem _station = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedRoleSystem _roleSystem = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DragonRuleComponent, AfterAntagEntitySelectedEvent>(AfterAntagEntitySelected);
        SubscribeLocalEvent<DragonRoleComponent, GetBriefingEvent>(UpdateBriefing);
    }

    private void UpdateBriefing(Entity<DragonRoleComponent> entity, ref GetBriefingEvent args)
    {
        var ent = args.Mind.Comp.OwnedEntity;

        if(ent is null)
            return;

        args.Append(MakeBriefing(ent.Value));
    }

    private void AfterAntagEntitySelected(Entity<DragonRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        if (!_mind.TryGetMind(args.EntityUid, out var mindId, out _))
            return;

        _roleSystem.MindHasRole<DragonRoleComponent>(mindId, out var dragonRole);

        if(dragonRole is null)
            return;

        _antag.SendBriefing(args.EntityUid, MakeBriefing(args.EntityUid), null, null);
    }

    private string MakeBriefing(EntityUid dragon)
    {
        var direction = string.Empty;

        var dragonXform = Transform(dragon);

        EntityUid? stationGrid = null;
        if (_station.GetStationInMap(dragonXform.MapID) is { } station)
            stationGrid = _station.GetLargestGrid(station);

        if (stationGrid is not null)
        {
            var stationPosition = _transform.GetWorldPosition((EntityUid)stationGrid);
            var dragonPosition = _transform.GetWorldPosition(dragon);

            var vectorToStation = stationPosition - dragonPosition;
            direction = ContentLocalizationManager.FormatDirection(vectorToStation.GetDir());
        }

        var briefing = Loc.GetString("dragon-role-briefing", ("direction", direction));

        return briefing;
    }
}
