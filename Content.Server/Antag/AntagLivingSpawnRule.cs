using Content.Server.Antag.Components;
using Content.Shared.GameTicking.Components;
using Content.Server.GameTicking.Rules;
using Robust.Shared.Map;
using System.Numerics;
using Content.Shared.Objectives.Systems;
using Robust.Shared.Random;

namespace Content.Server.Antag;

public sealed partial class AntagLivingSpawnSystem : GameRuleSystem<AntagLivingSpawnComponent>
{
    [Dependency] private TargetSystem _target = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AntagLivingSpawnComponent, AntagSelectLocationEvent>(OnSelectLocation);
    }

    protected override void Added(EntityUid uid, AntagLivingSpawnComponent comp, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        base.Added(uid, comp, gameRule, args);

        // get possible targets
        var allAliveHumanoids = _target.GetAliveHumans();

        // we already checked when starting the gamerule, but someone might have died since then.
        if (allAliveHumanoids.Count == 0)
        {
            Log.Warning("Could not find any alive players to spawn the antagonist on!");
            return;
        }

        // pick a random player
        var randomHumanoidMind = _random.Pick(allAliveHumanoids);
        if (randomHumanoidMind.Comp.OwnedEntity is null)
        {
            Log.Warning("Rolled a player without a body");
            return;
        }

        comp.Coords = new EntityCoordinates(randomHumanoidMind.Comp.OwnedEntity.Value, Vector2.Zero);
    }

    private void OnSelectLocation(Entity<AntagLivingSpawnComponent> ent, ref AntagSelectLocationEvent args)
    {
        if (ent.Comp.Coords != null)
            args.Coordinates.Add(ent.Comp.Coords.Value);
    }
}
