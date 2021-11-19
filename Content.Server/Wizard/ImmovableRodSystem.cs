using Robust.Shared.Maths;
using Content.Server.Body.Components;
using Content.Server.Damage.Components;
using Content.Server.Destructible;
using Content.Server.Popups;
using Content.Server.Throwing;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.Throwing;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Wizard;

public class ImmovableRodSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IMapManager _map = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDef = default!;

    /// <summary>
    ///     How often to check for a new targeT?
    /// </summary>
    private float _targetUpdateInterval = 5.0f;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // yes, we are deliberately including paused entities. fuck you
        foreach (var (rod, trans) in EntityManager.EntityQuery<ImmovableRodComponent, TransformComponent>(true))
        {
            if (!_map.TryGetGrid(trans.GridID, out var grid))
                continue;

            grid.SetTile(trans.Coordinates, Tile.Empty);

            foreach (var entity in )

            rod.Accumulator += frameTime;

            if (rod.Accumulator > _targetUpdateInterval)
            {
                rod.Accumulator -= _targetUpdateInterval;
                if (rod.Target == null)
                    continue;
            }
        }
    }

    public override void Initialize()
    {
        base.Initialize();

        EntityManager.EventBus.SubscribeLocalEvent<ImmovableRodComponent, ThrowDoHitEvent>(OnThrowDoHit, typeof(ImmovableRodComponent), new [] {typeof(DamageOtherOnHitComponent)});
        SubscribeLocalEvent<ImmovableRodComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<ImmovableRodComponent, ExaminedEvent>(OnExamined);
    }

    private void OnComponentInit(EntityUid uid, ImmovableRodComponent component, ComponentInit args)
    {
        if (EntityManager.TryGetComponent(uid, out PhysicsComponent? phys))
        {
            phys.LinearDamping = 0f;
            phys.Friction = 0f;
        }

        var ent = EntityManager.GetEntity(uid);
        var dir = new Vector2(_random.NextFloat() * 2 - 1, _random.NextFloat() * 2 - 1);
        ent.TryThrow(dir, 25f);
    }

    private void OnThrowDoHit(EntityUid uid, ImmovableRodComponent component, ThrowDoHitEvent args)
    {
        if (_random.Prob(component.HitSoundProbability))
        {
            SoundSystem.Play(Filter.Pvs(uid), component.Sound.GetSound(), uid, AudioParams.Default);
        }

        if (args.Target.Uid == component.Target)
        {
            // do something idk
        }

        if (EntityManager.HasComponent<ImmovableRodComponent>(args.Target.Uid))
        {
            // oh god.
            _popup.PopupEntity(Loc.GetString("immovable-rod-collided-rod-not-good"), uid, Filter.Pvs(uid));

            EntityManager.SpawnEntity("Singularity", args.Target.Transform.Coordinates);

            args.Target.QueueDelete();
            args.Thrown.QueueDelete();

            return;
        }

        // gib em
        if (EntityManager.TryGetComponent<BodyComponent>(args.Target.Uid, out var body))
        {
            component.MobCount++;

            _popup.PopupEntity(Loc.GetString("immovable-rod-penetrated-mob"), uid, Filter.Pvs(uid));
            body.Gib();
        }

        args.Target.QueueDelete();
    }

    private void OnExamined(EntityUid uid, ImmovableRodComponent component, ExaminedEvent args)
    {
        if (component.MobCount == 0)
        {
            args.PushText(Loc.GetString("immovable-rod-consumed-none"));
        }
        else
        {
            args.PushText(Loc.GetString("immovable-rod-consumed-souls", ("amount", component.MobCount)));
        }
    }
}
