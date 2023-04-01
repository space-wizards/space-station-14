using System.Linq;
using Content.Server.Damage.Systems;
using Content.Server.Hands.Components;
using Content.Server.Lightning;
using Content.Server.Lightning.Components;
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
    private int _sections = 1;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);

        SubscribeLocalEvent<InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<SurgeryRealmHeartComponent, CanWeightlessMoveEvent>(OnHeartCanWeightlessMove);
        SubscribeLocalEvent<SurgeryRealmProjectileComponent, StartCollideEvent>(OnProjectileCollide);
        SubscribeLocalEvent<SurgeryRealmAntiProjectileComponent, StartCollideEvent>(OnAntiProjectileCollide);
        SubscribeLocalEvent<SurgeryRealmOrangeProjectileComponent, StartCollideEvent>(OnOrangeProjectileCollide);
        SubscribeLocalEvent<SurgeryRealmHeartComponent, StartCollideEvent>(OnHeartCollide);

        SubscribeNetworkEvent<SurgeryRealmAcceptSelfEvent>(OnSurgeryRealmAcceptSelf);
    }

    private void OnOrangeProjectileCollide(EntityUid uid, SurgeryRealmOrangeProjectileComponent component, ref StartCollideEvent args)
    {
        if (HasComp<SurgeryRealmEdgeComponent>(args.OtherFixture.Body.Owner))
        {
            if (_timing.CurTick > MetaData(uid).CreationTick + 60)
                QueueDel(uid);
        }

        if (!TryComp(args.OtherFixture.Body.Owner, out SurgeryRealmHeartComponent? heart))
            return;

        if (!TryComp(heart.Owner, out InputMoverComponent? input) ||
            input.HeldMoveButtons != 0)
        {
            return;
        }

        SubtractHealth(heart);
    }

    private void SubtractHealth(SurgeryRealmHeartComponent heart)
    {
        if (heart.Health == 0)
            return;

        heart.Health--;
        Dirty(heart);

        if (heart.Health > 0)
            return;

        heart.Health = 0;

        if (!TryComp(heart.Camera, out ActorComponent? actor))
            return;

        StopOperation(actor.PlayerSession);
        QueueDel(heart.Owner);
    }

    private void OnHeartCollide(EntityUid uid, SurgeryRealmHeartComponent component, ref StartCollideEvent args)
    {
        if (!HasComp<LightningComponent>(args.OtherFixture.Body.Owner))
            return;

        SubtractHealth(component);
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

        _audio.Play(new SoundPathSpecifier("/Audio/Surgery/blast.ogg"), Filter.Empty().AddInRange(sliding.SectionPos, 10), sliding.Owner, true, AudioParams.Default.WithVolume(10));

        var slidingPos = _transform.GetWorldPosition(sliding.Owner);
        var y = sliding.SectionPos.Y + sliding.FinalY;
        _transform.SetWorldPosition(sliding.Owner, (slidingPos.X, y));

        Timer.Spawn(1000, () =>
        {
            var x = sliding.SectionPos.X - (slidingPos.X - sliding.SectionPos.X);
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
            if (_timing.CurTick > MetaData(uid).CreationTick + 60)
                QueueDel(uid);
        }

        if (!TryComp(args.OtherFixture.Body.Owner, out SurgeryRealmHeartComponent? heart))
            return;

        SubtractHealth(heart);
    }

    private void OnAntiProjectileCollide(EntityUid uid, SurgeryRealmAntiProjectileComponent component, ref StartCollideEvent args)
    {
        if (HasComp<SurgeryRealmEdgeComponent>(args.OtherFixture.Body.Owner))
        {
            if (_timing.CurTick > MetaData(uid).CreationTick + 60)
                QueueDel(uid);
        }

        if (!TryComp(args.OtherFixture.Body.Owner, out SurgeryRealmHeartComponent? heart))
            return;

        if (!TryComp(heart.Owner, out InputMoverComponent? input) ||
            input.HeldMoveButtons == 0)
        {
            return;
        }

        SubtractHealth(heart);
    }

    private void OnInteractUsing(InteractUsingEvent args)
    {
        if (!HasComp<SurgeryRealmToolComponent>(args.Used))
        {
            return;
        }

        if (args.User != args.Target)
        {
            if (!HasComp<SurgeryRealmToolDuelComponent>(args.Used))
                return;

            if (!TryComp(args.User, out ActorComponent? userActor) ||
                !TryComp(args.Target, out ActorComponent? targetActor))
            {
                return;
            }

            StartDuel(new List<IPlayerSession> {userActor.PlayerSession, targetActor.PlayerSession}, args.Used);

            return;
        }

        {
            if (HasComp<SurgeryRealmVictimComponent>(args.User) ||
                !TryComp(args.User, out ActorComponent? userActor))
            {
                return;
            }

            var ev = new SurgeryRealmRequestSelfEvent();
            RaiseNetworkEvent(ev, userActor.PlayerSession);
        }

    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        if (_surgeryRealmMap == MapId.Nullspace || !_maps.MapExists(_surgeryRealmMap))
            return;

        _maps.DeleteMap(_surgeryRealmMap);
        _surgeryRealmMap = MapId.Nullspace;
        _sections = 1;
    }

    public void StartOperation(IPlayerSession victimPlayer, EntityUid? toolId, SurgeryRealmMusic? music = null)
    {
        if (victimPlayer.AttachedEntity is not { } victimEntity)
            return;

        toolId ??= Spawn("Scalpel", Transform(victimEntity).Coordinates);
        var tool = EnsureComp<SurgeryRealmToolComponent>(toolId.Value);
        var victim = EnsureComp<SurgeryRealmVictimComponent>(victimEntity);
        victim.Tool = toolId.Value;

        EnsureMap();

        if (tool.Position == null || tool.Victims.Count == 0)
            tool.Position = new MapCoordinates(GetNextPosition(), _surgeryRealmMap);

        tool.Fight++;
        var fight = tool.Fight;

        tool.Victims.Add(victimPlayer);

        victim.Heart = Spawn(tool.HeartPrototype, tool.Position.Value.Offset(0, -5));
        _console.ExecuteCommand($"scale {victim.Heart} 2.5");

        var clown = Spawn("SurgeryRealmClown", tool.Position.Value.Offset(0, 3));
        _console.ExecuteCommand($"scale {clown} 5");

        SpawnEdges(tool.Position.Value);

        var camera = EntityManager.SpawnEntity("SurgeryRealmCamera", tool.Position.Value);
        EnsureComp<SurgeryRealmHeartComponent>(victim.Heart).Camera = camera;

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
                // case var x when x < 0.25:
                    // RaiseNetworkEvent(new SurgeryRealmStartEvent(camera), victimPlayer);
                    // break;
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
                // case SurgeryRealmMusic.Midi:
                //     RaiseNetworkEvent(new SurgeryRealmStartEvent(camera), victimPlayer);
                //     break;
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
            if (tool.Position == null || fight != tool.Fight)
                return;

            SpawnOppositeBananaWallsHoles(tool.Position.Value);
        });

        Timer.Spawn(17000, () =>
        {
            if (tool.Position == null || fight != tool.Fight)
                return;

            SpawnAlternatingBananaPillars(tool.Position.Value);
        });

        Timer.Spawn(32000, () =>
        {
            if (tool.Position == null || fight != tool.Fight)
                return;

            SpawnVerticallySlidingPdas(tool.Position.Value);
        });

        Timer.Spawn(45000, () =>
        {
            if (tool.Position == null || fight != tool.Fight)
                return;

            StopOperation(victimPlayer, true);
        });
    }

    public void StartDuel(List<IPlayerSession> victimPlayers, EntityUid? toolId, SurgeryRealmMusic? music = null)
    {
        if (victimPlayers.Count == 0)
            return;

        if (victimPlayers.Any(player => player.AttachedEntity == null))
            return;

        var victimEntities = victimPlayers.Select(player => player.AttachedEntity!.Value).ToArray();

        var firstPlayerEntity = victimEntities[0];
        toolId ??= Spawn("Scalpel", Transform(firstPlayerEntity).Coordinates);
        var tool = EnsureComp<SurgeryRealmToolComponent>(toolId.Value);
        tool.Fight++;
        var fight = tool.Fight;

        EnsureMap();

        if (tool.Position == null || tool.Victims.Count == 0)
            tool.Position = new MapCoordinates(GetNextPosition(), _surgeryRealmMap);

        tool.Victims.UnionWith(victimPlayers);

        var clown = Spawn("SurgeryRealmClown", tool.Position.Value.Offset(0, 3));
        _console.ExecuteCommand($"scale {clown} 5");

        SpawnEdges(tool.Position.Value);

        for (var i = 0; i < victimEntities.Length; i++)
        {
            var victimEntity = victimEntities[i];
            var victimPlayer = victimPlayers[i];
            var victim = EnsureComp<SurgeryRealmVictimComponent>(victimEntity);
            victim.Tool = toolId.Value;
            victim.Heart = Spawn(tool.HeartPrototype, tool.Position.Value.Offset(0, -5));
            _console.ExecuteCommand($"scale {victim.Heart} 2.5");

            var camera = EntityManager.SpawnEntity("SurgeryRealmCamera", tool.Position.Value);
            EnsureComp<SurgeryRealmHeartComponent>(victim.Heart).Camera = camera;

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
                    // case var x when x < 0.25:
                    //     if (i == 0)
                    //         RaiseNetworkEvent(new SurgeryRealmStartEvent(camera), victimPlayer);
                    //     break;
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
                    // case SurgeryRealmMusic.Midi:
                    //     if (i == 0)
                    //         RaiseNetworkEvent(new SurgeryRealmStartEvent(camera), victimPlayer);
                    //     break;
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
        }

        void A(float speedMultiplier)
        {
            Timer.Spawn(2000, () =>
            {
                if (tool.Position == null || fight != tool.Fight)
                    return;

                SpawnOppositeBananaWallsHoles(tool.Position.Value, speedMultiplier);
            });

            Timer.Spawn(17000, () =>
            {
                if (tool.Position == null || fight != tool.Fight)
                    return;

                SpawnAlternatingBananaPillars(tool.Position.Value, speedMultiplier);
            });

            var thirdStage = (int) (17000 + 15000 / speedMultiplier);
            Timer.Spawn(thirdStage, () =>
            {
                if (tool.Position == null || fight != tool.Fight)
                    return;

                SpawnVerticallySlidingPdas(tool.Position.Value, speedMultiplier);
            });

            Timer.Spawn(thirdStage + 13000, () =>
            {
                if (fight != tool.Fight)
                    return;

                if (tool.Victims.Count > 1 && tool.Position != null && fight == tool.Fight)
                {
                    A(speedMultiplier * 2);
                }
                else
                {
                    foreach (var toolVictim in tool.Victims)
                    {
                        if (tool.Victims.Count == 1)
                        {
                            StopOperation(toolVictim, true);
                        }
                        else
                        {
                            StopOperation(toolVictim);
                        }
                    }
                }
            });
        }

        A(1);
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

    private void SpawnOppositeBananaWallsHoles(MapCoordinates coordinates, float speedMultiplier = 1, bool chain = true)
    {
        var xSpeed = 4f * speedMultiplier;
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
                Timer.Spawn(1500 * i, () => SpawnOppositeBananaWallsHoles(coordinates, speedMultiplier, false));
            }
        }
    }

    private void SpawnAlternatingBananaPillars(MapCoordinates coordinates, float speedMultiplier = 1, bool chain = true)
    {
        var xSpeed = 8f * speedMultiplier;

        for (var y = -6; y < -1; y++)
        {
            var projectile1 = Spawn("SurgeryRealmBananaBlueProjectile", coordinates.Offset(6, y - 0.25f));
            var projectile2 = Spawn("SurgeryRealmBananaBlueProjectile", coordinates.Offset(6, y + 0.25f));

            Physics.SetLinearVelocity(projectile1, new Vector2(-xSpeed, 0));
            Physics.SetLinearVelocity(projectile2, new Vector2(-xSpeed, 0));
        }

        Timer.Spawn(500, () =>
        {
            for (var y = -6; y < -1; y++)
            {
                var projectile1 = Spawn("SurgeryRealmBananaOrangeProjectile", coordinates.Offset(6, y - 0.25f));
                var projectile2 = Spawn("SurgeryRealmBananaOrangeProjectile", coordinates.Offset(6, y + 0.25f));

                Physics.SetLinearVelocity(projectile1, new Vector2(-xSpeed, 0));
                Physics.SetLinearVelocity(projectile2, new Vector2(-xSpeed, 0));
            }
        });

        if (chain)
        {
            for (var i = 1; i < 9; i++)
            {
                Timer.Spawn(1500 * i, () => SpawnAlternatingBananaPillars(coordinates, speedMultiplier, false));
            }
        }
    }

    private void SpawnVerticallySlidingPdas(MapCoordinates coordinates, float speedMultiplier = 1)
    {
        var yPositions = new[] { -5, -5, -4, -5, -5, -4, -3, -3, -4 };
        for (var i = 1; i < 10; i++)
        {
            var i1 = i;
            Timer.Spawn((int) (1000 * i1 / speedMultiplier), () =>
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

    public void StopOperation(IPlayerSession victimPlayer, bool successful = false)
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
            if (successful)
                RaiseLocalEvent(victimEntity, new RejuvenateEvent());

            if (TryComp(victim.Tool, out SurgeryRealmToolComponent? tool))
            {
                tool.Victims.Remove(victimPlayer);
                if (tool.Victims.Count == 0)
                    tool.Position = null;
            }
        }

        RemComp<SurgeryRealmVictimComponent>(victimEntity);

        camera.OldEntity = null;
        QueueDel(camera.Owner);
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
