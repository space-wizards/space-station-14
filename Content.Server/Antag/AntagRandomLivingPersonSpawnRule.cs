using Content.Server.Antag.Components;
using Content.Shared.GameTicking.Components;
using Content.Server.GameTicking.Rules;
using Content.Shared.Objectives.Systems;
using Robust.Shared.Random;

namespace Content.Server.Antag;

public sealed partial class AntagRandomLivingPersonSpawnSystem : GameRuleSystem<AntagRandomLivingPersonSpawnComponent>
{
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private TargetSystem _target = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IEntityManager _entMan = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AntagRandomLivingPersonSpawnComponent, AntagSelectLocationEvent>(OnSelectLocation);
    }

    protected override void Added(EntityUid uid, AntagRandomLivingPersonSpawnComponent comp, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        base.Added(uid, comp, gameRule, args);

        var allAliveHumanoids = _target.GetAliveHumans();

        // we already checked when starting the gamerule, but someone might have died since then.
        if (allAliveHumanoids.Count == 0)
        {
            Log.Warning("Could not find any alive players to create a paradox clone from!");
            return;
        }

        // pick a random player
        var randomHumanoidMind = _random.Pick(allAliveHumanoids);

        var entity = randomHumanoidMind.Comp.OwnedEntity;

        comp.Target = entity;
        comp.Mind = randomHumanoidMind;
    }

    private void OnSelectLocation(Entity<AntagRandomLivingPersonSpawnComponent> ent, ref AntagSelectLocationEvent args)
    {
        if (ent.Comp.Target != null)
        {
            if (_entMan.TryGetComponent<TransformComponent>(ent.Comp.Target, out var transform))
            {
                args.Coordinates.Add(_transform.GetMapCoordinates(transform));
            }
        }
    }
}
