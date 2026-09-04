using Content.Server.Antag.Components;
using Content.Server.Station.Systems;
using Content.Shared.Antag;
using Content.Shared.GameTicking.Components;
using Content.Shared.GameTicking.Rules;

namespace Content.Server.Antag;

public sealed partial class AntagRandomSpawnSystem : GameRuleSystem<AntagRandomSpawnComponent>
{
    [Dependency] private ServerStationSystem _station = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AntagRandomSpawnComponent, AntagSelectLocationEvent>(OnSelectLocation);
    }

    protected override void Added(Entity<AntagRandomSpawnComponent, GameRuleComponent> rule, ref GameRuleAddedEvent args)
    {
        base.Added(rule, ref args);

        // we have to select this here because AntagSelectLocationEvent is raised twice because MakeAntag is called twice
        // once when a ghost role spawner is created and once when someone takes the ghost role

        if (_station.TryFindRandomTile(out _, out _, out _, out var coords))
            rule.Comp1.Coords = coords;
    }

    private void OnSelectLocation(Entity<AntagRandomSpawnComponent> ent, ref AntagSelectLocationEvent args)
    {
        if (ent.Comp.Coords != null)
            args.Coordinates.Add(_transform.ToMapCoordinates(ent.Comp.Coords.Value));
    }
}
