using Content.Server.Damage.Systems;
using Content.Server.Hands.Components;
using Content.Server.Lightning;
using Content.Server.Mind.Components;
using Content.Server.Physics.Controllers;
using Content.Shared.GameTicking;
using Content.Shared.Interaction;
using Content.Shared.Medical.Surgery;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Rejuvenate;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Medical.Surgery;

public sealed class SurgeryRealmSystem : SharedSurgeryRealmSystem
{
    private const int SectionSeparation = 100;

    [Dependency] private readonly IConsoleHost _console = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IMapManager _maps = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly LightningSystem _lightning = default!;
    [Dependency] private readonly GodmodeSystem _godmode = default!;
    [Dependency] private readonly MoverController _mover = default!;
    [Dependency] private readonly ViewSubscriberSystem _viewSubscriber = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    private MapId _surgeryRealmMap = MapId.Nullspace;
    private int _sections;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);

        SubscribeLocalEvent<InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<SurgeryRealmHeartComponent, CanWeightlessMoveEvent>(OnHeartCanWeightlessMove);
        SubscribeLocalEvent<SurgeryRealmProjectileComponent, StartCollideEvent>(OnProjectileCollide);
        SubscribeLocalEvent<SurgeryRealmAntiProjectileComponent, StartCollideEvent>(OnAntiProjectileCollide);

        SubscribeNetworkEvent<SurgeryRealmAcceptSelfEvent>(OnSurgeryRealmAcceptSelf);
    }

    private void OnSurgeryRealmAcceptSelf(SurgeryRealmAcceptSelfEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } playerEntity)
            return;

        if (!TryComp(playerEntity, out HandsComponent? hands) ||
            !hands.Hands.TryFirstOrNull(hand => hand.Value.HeldEntity != null && HasComp<SurgeryRealmToolComponent>(hand.Value.HeldEntity), out var tool))
        {
            return;
        }

        StartOperation((IPlayerSession) args.SenderSession, tool.Value.Value.HeldEntity!.Value);
    }

    protected override void Fire(SurgeryRealmSlidingComponent sliding)
    {
        base.Fire(sliding);

        sliding.Fired = true;

        _audio.Play(new SoundPathSpecifier("/Audio/Surgery/blast.ogg"), Filter.Empty().AddInRange(sliding.SectionPos, 10), sliding.Owner, true);

        Timer.Spawn(1000, () =>
        {
            var slidingPos = _transform.GetWorldPosition(sliding.Owner);
            var x = -(sliding.SectionPos.X + slidingPos.X) * 2;
            var y = sliding.SectionPos.Y + sliding.FinalY;
            var opposite = Spawn("", new MapCoordinates(x, y, sliding.SectionPos.MapId));
            var controller = Spawn("SurgeryRealmVirtualBeamEntityController", sliding.SectionPos);

            _lightning.ShootLightning(sliding.Owner, opposite, "SurgeryRealmLightning", controller);

            var physX = slidingPos.X > sliding.SectionPos.X ? 10 : -10;
            Physics.SetLinearVelocity(sliding.Owner, (physX, 0));

            Timer.Spawn(500, () =>
            {
                QueueDel(sliding.Owner);
            });
        });
    }

    private void OnHeartCanWeightlessMove(EntityUid uid, SurgeryRealmHeartComponent component, ref CanWeightlessMoveEvent args)
    {
        args.CanMove = true;
    }

    private void OnProjectileCollide(EntityUid uid, SurgeryRealmProjectileComponent component, ref StartCollideEvent args)
    {
        if (HasComp<SurgeryRealmEdgeComponent>(args.OtherFixture.Body.Owner))
        {
            if (_timing.CurTick > MetaData(uid).CreationTick + 90)
                QueueDel(uid);
        }

        if (!TryComp(args.OtherFixture.Body.Owner, out SurgeryRealmHeartComponent? heart))
            return;

        heart.Health--;
        Dirty(heart);

        if (heart.Health > 0)
            return;

        heart.Health = 0;

        if (!TryComp(heart.Owner, out ActorComponent? actor))
            return;

        StopOperation(actor.PlayerSession);
    }

    private void OnAntiProjectileCollide(EntityUid uid, SurgeryRealmAntiProjectileComponent component, ref StartCollideEvent args)
    {
        if (HasComp<SurgeryRealmEdgeComponent>(args.OtherFixture.Body.Owner))
        {
            if (_timing.CurTick > MetaData(uid).CreationTick + 90)
                QueueDel(uid);
        }

        if (!TryComp(args.OtherFixture.Body.Owner, out SurgeryRealmHeartComponent? heart))
            return;

        if (!TryComp(heart.Owner, out InputMoverComponent? input) ||
            input.HeldMoveButtons == 0)
        {
            return;
        }

        heart.Health--;
        Dirty(heart);

        if (heart.Health > 0)
            return;

        heart.Health = 0;

        if (!TryComp(heart.Owner, out ActorComponent? actor))
            return;

        StopOperation(actor.PlayerSession);
    }

    private void OnInteractUsing(InteractUsingEvent args)
    {
        if (!HasComp<SurgeryRealmToolComponent>(args.Used))
        {
            return;
        }

        if (args.User == args.Target)
        {
            if (!HasComp<SurgeryRealmVictimComponent>(args.User) &&
                TryComp(args.User, out ActorComponent? userActor))
            {
                var ev = new SurgeryRealmRequestSelfEvent();
                RaiseNetworkEvent(ev, userActor.PlayerSession);
            }
        }
        else
        {
            if (!HasComp<SurgeryRealmVictimComponent>(args.User) &&
                TryComp(args.User, out ActorComponent? userActor))
            {
                StartOperation(userActor.PlayerSession, args.Used);
            }

            if (HasComp<SurgeryRealmVictimComponent>(args.Target) ||
                !TryComp(args.Target, out ActorComponent? targetActor))
            {
                return;
            }

            StartOperation(targetActor.PlayerSession, args.Used);
        }
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        if (_surgeryRealmMap == MapId.Nullspace || !_maps.MapExists(_surgeryRealmMap))
            return;

        _maps.DeleteMap(_surgeryRealmMap);
        _surgeryRealmMap = MapId.Nullspace;
        _sections = 0;
    }

    public void StartOperation(IPlayerSession victimPlayer, EntityUid? toolId, SurgeryRealmMusic? music = null)
    {
        if (victimPlayer.AttachedEntity is not { } victimEntity)
            return;

        toolId ??= Spawn("Scalpel", Transform(victimEntity).Coordinates);
        var tool = EnsureComp<SurgeryRealmToolComponent>(toolId.Value);
        var victim = EnsureComp<SurgeryRealmVictimComponent>(victimEntity);

        EnsureMap();

        if (tool.Position == null || tool.Victims.Count == 0)
            tool.Position = new MapCoordinates(GetNextPosition(), _surgeryRealmMap);

        tool.Victims.Add(victimEntity);

        victim.Heart = Spawn(tool.HeartPrototype, tool.Position.Value.Offset(0, -5));
        _console.ExecuteCommand($"scale {victim.Heart} 2.5");

        var clown = Spawn("SurgeryRealmClown", tool.Position.Value.Offset(0, 3));
        _console.ExecuteCommand($"scale {clown} 5");

        SpawnEdges(tool.Position.Value);

        var camera = EntityManager.SpawnEntity("SurgeryRealmCamera", tool.Position.Value);

        var mind = EnsureComp<MindComponent>(victimEntity);
        var cameraComp = EnsureComp<SurgeryRealmCameraComponent>(camera);
        cameraComp.OldEntity = victimEntity;
        cameraComp.Mind = mind.Mind;

        var eyeComponent = EnsureComp<EyeComponent>(camera);

        eyeComponent.DrawFov = false;
        _viewSubscriber.AddViewSubscriber(camera, victimPlayer);

        _mover.SetRelay(camera, victim.Heart);

        _godmode.EnableGodmode(victimEntity);

        mind.Mind?.Visit(camera);

        if (music == null)
        {
            switch (_random.NextFloat())
            {
                case var x when x < 0.25:
                    RaiseNetworkEvent(new SurgeryRealmStartEvent(camera), victimPlayer);
                    break;
                case var x when x < 0.95:
                    _audio.PlayEntity(new SoundPathSpecifier("/Audio/Surgery/megalovania.ogg"), camera, camera, AudioParams.Default.WithVolume(2));
                    break;
                default:
                    _audio.PlayEntity(new SoundPathSpecifier("/Audio/Surgery/undermale.ogg"), camera, camera, AudioParams.Default.WithVolume(6));
                    break;
            }
        }
        else
        {
            switch (music)
            {
                case SurgeryRealmMusic.Midi:
                    RaiseNetworkEvent(new SurgeryRealmStartEvent(camera), victimPlayer);
                    break;
                case SurgeryRealmMusic.Megalovania:
                    _audio.PlayEntity(new SoundPathSpecifier("/Audio/Surgery/megalovania.ogg"), camera, camera, AudioParams.Default.WithVolume(2));
                    break;
                case SurgeryRealmMusic.Undermale:
                    _audio.PlayEntity(new SoundPathSpecifier("/Audio/Surgery/undermale.ogg"), camera, camera, AudioParams.Default.WithVolume(6));
                    break;
                case null:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(music), music, null);
            }
        }

        Timer.Spawn(2000, () =>
        {
            SpawnOppositeBananaWallsHoles(tool.Position.Value);
        });

        Timer.Spawn(17000, () =>
        {
            SpawnAlternatingBananaPillars(tool.Position.Value);
        });

        Timer.Spawn(32000, () =>
        {
            SpawnVerticallySlidingPdas(tool.Position.Value);
        });
    }

    private void SpawnEdges(MapCoordinates coordinates)
    {
        for (var x = -5; x < 6; x++)
        {
            Spawn("SurgeryRealmEdge", coordinates.Offset(x, -1));
            Spawn("SurgeryRealmEdge", coordinates.Offset(x, -7));
        }

        for (var y = -7; y < 0; y++)
        {
            Spawn("SurgeryRealmEdge", coordinates.Offset(6, y));
            Spawn("SurgeryRealmEdge", coordinates.Offset(-6, y));
        }
    }

    private void SpawnOppositeBananaWallsHoles(MapCoordinates coordinates, bool chain = true)
    {
        const float xSpeed = 4f;
        var skip = _random.Next(-5, -1);

        for (var y = -6; y < -1; y++)
        {
            if (y == skip)
                continue;

            var projectile1 = Spawn("SurgeryRealmBananaProjectile", coordinates.Offset(-6, y - 0.25f));
            var projectile2 = Spawn("SurgeryRealmBananaProjectile", coordinates.Offset(-6, y + 0.25f));

            Physics.SetLinearVelocity(projectile1, new Vector2(xSpeed, 0));
            Physics.SetLinearVelocity(projectile2, new Vector2(xSpeed, 0));
        }

        for (var y = -6; y < -1; y++)
        {
            if (y == skip)
                continue;

            var projectile1 = Spawn("SurgeryRealmBananaProjectile", coordinates.Offset(6, y - 0.25f));
            var projectile2 = Spawn("SurgeryRealmBananaProjectile", coordinates.Offset(6, y + 0.25f));

            Physics.SetLinearVelocity(projectile1, new Vector2(-xSpeed, 0));
            Physics.SetLinearVelocity(projectile2, new Vector2(-xSpeed, 0));
        }

        if (chain)
        {
            for (var i = 1; i < 9; i++)
            {
                Timer.Spawn(1500 * i, () => SpawnOppositeBananaWallsHoles(coordinates, false));
            }
        }
    }

    private void SpawnAlternatingBananaPillars(MapCoordinates coordinates, bool chain = true)
    {
        const float xSpeed = 8f;

        for (var y = -6; y < -1; y++)
        {
            var projectile1 = Spawn("SurgeryRealmBananaBlueProjectile", coordinates.Offset(6, y - 0.25f));
            var projectile2 = Spawn("SurgeryRealmBananaBlueProjectile", coordinates.Offset(6, y + 0.25f));

            Physics.SetLinearVelocity(projectile1, new Vector2(-xSpeed, 0));
            Physics.SetLinearVelocity(projectile2, new Vector2(-xSpeed, 0));
        }

        Timer.Spawn(500, () =>
        {
            var projectile2 = Spawn("SurgeryRealmBananaProjectile", coordinates.Offset(6, -6 - 0.25f));
            var projectile3 = Spawn("SurgeryRealmBananaProjectile", coordinates.Offset(6, -6 + 0.25f));

            Physics.SetLinearVelocity(projectile2, new Vector2(-xSpeed, 0));
            Physics.SetLinearVelocity(projectile3, new Vector2(-xSpeed, 0));
        });

        if (chain)
        {
            for (var i = 1; i < 9; i++)
            {
                Timer.Spawn(1500 * i, () => SpawnAlternatingBananaPillars(coordinates, false));
            }
        }
    }

    private void SpawnVerticallySlidingPdas(MapCoordinates coordinates)
    {
        var yPositions = new[] { -5, -5, -4, -5, -5, -4, -3, -3, -4 };
        for (var i = 1; i < 10; i++)
        {
            var i1 = i;
            Timer.Spawn(1000 * i1, () =>
            {
                var x = i1 % 2 == 0 ? 8 : -8;
                SpawnSinglePda(coordinates, (x, yPositions[i1 - 1]));
            });
        }
    }

    private void SpawnSinglePda(MapCoordinates coordinates, Vector2 pos)
    {
        var pda = Spawn("SurgeryRealmPDA", coordinates.Offset(pos.X, 20));
        _console.ExecuteCommand($"scale {pda} 2");

        var sliding = EnsureComp<SurgeryRealmSlidingComponent>(pda);
        sliding.FinalY = pos.Y;
        sliding.SectionPos = coordinates;

        Physics.SetLinearVelocity(pda, new Vector2(0, -20));
    }

    public void StopOperation(IPlayerSession victimPlayer)
    {
        if (victimPlayer.AttachedEntity is not { } victimEntity)
            return;

        if (!TryComp(victimEntity, out SurgeryRealmCameraComponent? camera))
            return;

        if (Deleted(camera.OldEntity))
            return;

        camera.Mind?.UnVisit();

        victimEntity = victimPlayer.AttachedEntity.Value;
        _godmode.DisableGodmode(victimEntity);

        if (TryComp(victimEntity, out SurgeryRealmVictimComponent? victim))
        {
            if (victim.Successful)
                RaiseLocalEvent(victimEntity, new RejuvenateEvent());

            if (TryComp(victim.Tool, out SurgeryRealmToolComponent? tool))
                tool.Victims.Remove(victimEntity);
        }

        RemComp<SurgeryRealmVictimComponent>(victimEntity);
    }

    private void EnsureMap()
    {
        if (_surgeryRealmMap != MapId.Nullspace && _maps.MapExists(_surgeryRealmMap))
            return;

        _surgeryRealmMap = _maps.CreateMap();
        var map = Comp<MapComponent>(_maps.GetMapEntityId(_surgeryRealmMap));

        map.LightingEnabled = false;
        Dirty(map);
    }

    // Copied from TabletopSystem
    private Vector2 GetNextPosition()
    {
        return UlamSpiral(_sections++) * SectionSeparation;
    }

    private Vector2i UlamSpiral(int n)
    {
        var k = (int)MathF.Ceiling(MathF.Sqrt(n) - 1) / 2;
        var t = 2 * k + 1;
        var m = (int)MathF.Pow(t, 2);
        t--;

        if (n >= m - t)
            return new Vector2i(k - (m - n), -k);

        m -= t;

        if (n >= m - t)
            return new Vector2i(-k, -k + (m - n));

        m -= t;

        if (n >= m - t)
            return new Vector2i(-k + (m - n), k);

        return new Vector2i(k, k - (m - n - t));
    }
}
