using System.Threading;
using Content.Server.Administration.Components;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Clothing.Systems;
using Content.Server.Electrocution;
using Content.Server.Explosion.EntitySystems;
using Content.Server.GhostKick;
using Content.Server.Medical;
using Content.Server.Nutrition.EntitySystems;
using Content.Server.Pointing.Components;
using Content.Server.Polymorph.Systems;
using Content.Server.Popups;
using Content.Server.Speech.Components;
using Content.Server.Storage.Components;
using Content.Server.Storage.EntitySystems;
using Content.Server.Tabletop;
using Content.Server.Tabletop.Components;
using Content.Shared.Administration;
using Content.Shared.Administration.Components;
using Content.Shared.Administration.Prototypes;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Clumsy;
using Content.Shared.Clothing.Components;
using Content.Shared.Cluwne;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.Electrocution;
using Content.Shared.EntityEffects;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Content.Shared.Slippery;
using Content.Shared.Stunnable;
using Content.Shared.Tabletop.Components;
using Content.Shared.Tools.Systems;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.Administration.Systems;

public sealed partial class AdminVerbSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
    [Dependency] private readonly BodySystem _bodySystem = default!;
    [Dependency] private readonly CreamPieSystem _creamPieSystem = default!;
    [Dependency] private readonly EntityStorageSystem _entityStorageSystem = default!;
    [Dependency] private readonly ExplosionSystem _explosionSystem = default!;
    [Dependency] private readonly FixtureSystem _fixtures = default!;
    [Dependency] private readonly GhostKickManager _ghostKickManager = default!;
    [Dependency] private readonly SharedGodmodeSystem _sharedGodmodeSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly TabletopSystem _tabletopSystem = default!;
    [Dependency] private readonly VomitSystem _vomitSystem = default!;
    [Dependency] private readonly WeldableSystem _weldableSystem = default!;
    [Dependency] private readonly SharedContentEyeSystem _eyeSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly SuperBonkSystem _superBonkSystem = default!;
    [Dependency] private readonly SlipperySystem _slipperySystem = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    // All smite verbs have names so invokeverb works.
    private void AddSmiteVerbs(GetVerbsEvent<Verb> args)
    {
        if (!TryComp(args.User, out ActorComponent? actor))
            return;

        var player = actor.PlayerSession;

        if (!_adminManager.HasAdminFlag(player, AdminFlags.Fun))
            return;

        // 1984.
        if (HasComp<MapComponent>(args.Target) || HasComp<MapGridComponent>(args.Target))
            return;

        foreach (var smite in _proto.EnumeratePrototypes<AdminSmitePrototype>())
        {
            if (smite.Whitelist is not null && !_whitelist.IsValid(smite.Whitelist, args.Target))
                continue;

            Verb verb = new()
            {
                Text = Loc.GetString(smite.Name),
                Message = Loc.GetString(smite.Name) + ": " + Loc.GetString(smite.Desc),
                Icon = smite.Icon,
                Category = VerbCategory.Smite,
                Impact = LogImpact.Extreme,
                Act = () =>
                {
                    foreach (var effect in smite.Effects)
                    {
                        var effectArgs = new EntityEffectBaseArgs(args.Target, EntityManager);

                        if (!effect.ShouldApply(effectArgs))
                            continue;

                        effect.Effect(effectArgs);
                    }

                    EntityManager.AddComponents(args.Target, smite.Components);


                    if (smite.PopupMessage is not null)
                    {
                        _popupSystem.PopupEntity(Loc.GetString(smite.PopupMessage.Value), args.Target, args.Target, PopupType.LargeCaution);
                    }
                }
            };
            args.Verbs.Add(verb);
        }

        return;

        var explodeName = Loc.GetString("admin-smite-explode-name").ToLowerInvariant();
        Verb explode = new()
        {
            Text = explodeName,
            Act = () =>
            {
                var coords = _transformSystem.GetMapCoordinates(args.Target);
                Timer.Spawn(_gameTiming.TickPeriod,
                    () => _explosionSystem.QueueExplosion(coords, ExplosionSystem.DefaultExplosionPrototypeId,
                        4, 1, 2, args.Target, maxTileBreak: 0), // it gibs, damage doesn't need to be high.
                    CancellationToken.None);

                _bodySystem.GibBody(args.Target);
            },
            Message = string.Join(": ", explodeName, Loc.GetString("admin-smite-explode-description")) // we do this so the description tells admins the Text to run it via console.
        };
        args.Verbs.Add(explode);

        var chessName = Loc.GetString("admin-smite-chess-dimension-name").ToLowerInvariant();
        Verb chess = new()
        {
            Text = chessName,
            Act = () =>
            {
                _sharedGodmodeSystem.EnableGodmode(args.Target); // So they don't suffocate.
                EnsureComp<TabletopDraggableComponent>(args.Target);
                RemComp<PhysicsComponent>(args.Target); // So they can be dragged around.
                var xform = Transform(args.Target);
                _popupSystem.PopupEntity(Loc.GetString("admin-smite-chess-self"), args.Target,
                    args.Target, PopupType.LargeCaution);
                _popupSystem.PopupCoordinates(
                    Loc.GetString("admin-smite-chess-others", ("name", args.Target)), xform.Coordinates,
                    Filter.PvsExcept(args.Target), true, PopupType.MediumCaution);
                var board = Spawn("ChessBoard", xform.Coordinates);
                var session = _tabletopSystem.EnsureSession(Comp<TabletopGameComponent>(board));
                _transformSystem.SetMapCoordinates(args.Target, session.Position);
                _transformSystem.SetWorldRotationNoLerp((args.Target, xform), Angle.Zero);
            },
            Impact = LogImpact.Extreme,
            Message = string.Join(": ", chessName, Loc.GetString("admin-smite-chess-dimension-description"))
        };
        args.Verbs.Add(chess);

        if (TryComp<CreamPiedComponent>(args.Target, out var creamPied))
        {
            var creamPieName = Loc.GetString("admin-smite-creampie-name").ToLowerInvariant();
            Verb creamPie = new()
            {
                Text = creamPieName,
                Act = () =>
                {
                    _creamPieSystem.SetCreamPied(args.Target, creamPied, true);
                },
                Impact = LogImpact.Extreme,
                Message = string.Join(": ", creamPieName, Loc.GetString("admin-smite-creampie-description"))
            };
            args.Verbs.Add(creamPie);
        }

        if (TryComp<BloodstreamComponent>(args.Target, out var bloodstream))
        {
            var bloodRemovalName = Loc.GetString("admin-smite-remove-blood-name").ToLowerInvariant();
            Verb bloodRemoval = new()
            {
                Text = bloodRemovalName,
                Act = () =>
                {
                    _bloodstreamSystem.SpillAllSolutions((args.Target, bloodstream));
                    var xform = Transform(args.Target);
                    _popupSystem.PopupEntity(Loc.GetString("admin-smite-remove-blood-self"), args.Target,
                        args.Target, PopupType.LargeCaution);
                    _popupSystem.PopupCoordinates(Loc.GetString("admin-smite-remove-blood-others", ("name", args.Target)), xform.Coordinates,
                        Filter.PvsExcept(args.Target), true, PopupType.MediumCaution);
                },
                Impact = LogImpact.Extreme,
                Message = string.Join(": ", bloodRemovalName, Loc.GetString("admin-smite-remove-blood-description"))
            };
            args.Verbs.Add(bloodRemoval);
        }

        // bobby...
        if (TryComp<BodyComponent>(args.Target, out var body))
        {
            var vomitOrgansName = Loc.GetString("admin-smite-vomit-organs-name").ToLowerInvariant();
            Verb vomitOrgans = new()
            {
                Text = vomitOrgansName,
                Act = () =>
                {
                    _vomitSystem.Vomit(args.Target, -1000, -1000); // You feel hollow!
                    var organs = _bodySystem.GetBodyOrganEntityComps<TransformComponent>((args.Target, body));
                    var baseXform = Transform(args.Target);
                    foreach (var organ in organs)
                    {
                        if (HasComp<BrainComponent>(organ.Owner) || HasComp<EyeComponent>(organ.Owner))
                            continue;

                        _transformSystem.PlaceNextTo((organ.Owner, organ.Comp1), (args.Target, baseXform));
                    }

                    _popupSystem.PopupEntity(Loc.GetString("admin-smite-vomit-organs-self"), args.Target,
                        args.Target, PopupType.LargeCaution);
                    _popupSystem.PopupCoordinates(Loc.GetString("admin-smite-vomit-organs-others", ("name", args.Target)), baseXform.Coordinates,
                        Filter.PvsExcept(args.Target), true, PopupType.MediumCaution);
                },
                Impact = LogImpact.Extreme,
                Message = string.Join(": ", vomitOrgansName, Loc.GetString("admin-smite-vomit-organs-description"))
            };
            args.Verbs.Add(vomitOrgans);

            var handsRemovalName = Loc.GetString("admin-smite-remove-hands-name").ToLowerInvariant();
            Verb handsRemoval = new()
            {
                Text = handsRemovalName,
                Act = () =>
                {
                    var baseXform = Transform(args.Target);
                    foreach (var part in _bodySystem.GetBodyChildrenOfType(args.Target, BodyPartType.Hand))
                    {
                        _transformSystem.AttachToGridOrMap(part.Id);
                    }
                    _popupSystem.PopupEntity(Loc.GetString("admin-smite-remove-hands-self"), args.Target,
                        args.Target, PopupType.LargeCaution);
                    _popupSystem.PopupCoordinates(Loc.GetString("admin-smite-remove-hands-other", ("name", args.Target)), baseXform.Coordinates,
                        Filter.PvsExcept(args.Target), true, PopupType.Medium);
                },
                Impact = LogImpact.Extreme,
                Message = string.Join(": ", handsRemovalName, Loc.GetString("admin-smite-remove-hands-description"))
            };
            args.Verbs.Add(handsRemoval);

            var stomachRemovalName = Loc.GetString("admin-smite-stomach-removal-name").ToLowerInvariant();
            Verb stomachRemoval = new()
            {
                Text = stomachRemovalName,
                Act = () =>
                {
                    foreach (var entity in _bodySystem.GetBodyOrganEntityComps<StomachComponent>((args.Target, body)))
                    {
                        QueueDel(entity.Owner);
                    }

                    _popupSystem.PopupEntity(Loc.GetString("admin-smite-stomach-removal-self"), args.Target,
                        args.Target, PopupType.LargeCaution);
                },
                Impact = LogImpact.Extreme,
                Message = string.Join(": ", stomachRemovalName, Loc.GetString("admin-smite-stomach-removal-description"))
            };
            args.Verbs.Add(stomachRemoval);

            var lungRemovalName = Loc.GetString("admin-smite-lung-removal-name").ToLowerInvariant();
            Verb lungRemoval = new()
            {
                Text = lungRemovalName,
                Act = () =>
                {
                    foreach (var entity in _bodySystem.GetBodyOrganEntityComps<LungComponent>((args.Target, body)))
                    {
                        QueueDel(entity.Owner);
                    }

                    _popupSystem.PopupEntity(Loc.GetString("admin-smite-lung-removal-self"), args.Target,
                        args.Target, PopupType.LargeCaution);
                },
                Impact = LogImpact.Extreme,
                Message = string.Join(": ", lungRemovalName, Loc.GetString("admin-smite-lung-removal-description"))
            };
            args.Verbs.Add(lungRemoval);
        }

        if (TryComp<PhysicsComponent>(args.Target, out var physics))
        {
            var pinballName = Loc.GetString("admin-smite-pinball-name").ToLowerInvariant();
            Verb pinball = new()
            {
                Text = pinballName,
                Act = () =>
                {
                    var xform = Transform(args.Target);
                    var fixtures = Comp<FixturesComponent>(args.Target);
                    _transformSystem.Unanchor(args.Target, xform); // Just in case.
                    _physics.SetBodyType(args.Target, BodyType.Dynamic, manager: fixtures, body: physics);
                    _physics.SetBodyStatus(args.Target, physics, BodyStatus.InAir);
                    _physics.WakeBody(args.Target, manager: fixtures, body: physics);

                    foreach (var fixture in fixtures.Fixtures.Values)
                    {
                        if (!fixture.Hard)
                            continue;

                        _physics.SetRestitution(args.Target, fixture, 1.1f, false, fixtures);
                    }

                    _fixtures.FixtureUpdate(args.Target, manager: fixtures, body: physics);

                    _physics.SetLinearVelocity(args.Target, _random.NextVector2(1.5f, 1.5f), manager: fixtures, body: physics);
                    _physics.SetAngularVelocity(args.Target, MathF.PI * 12, manager: fixtures, body: physics);
                    _physics.SetLinearDamping(args.Target, physics, 0f);
                    _physics.SetAngularDamping(args.Target, physics, 0f);
                },
                Impact = LogImpact.Extreme,
                Message = string.Join(": ", pinballName, Loc.GetString("admin-smite-pinball-description"))
            };
            args.Verbs.Add(pinball);

            var yeetName = Loc.GetString("admin-smite-yeet-name").ToLowerInvariant();
            Verb yeet = new()
            {
                Text = yeetName,
                Act = () =>
                {
                    var xform = Transform(args.Target);
                    var fixtures = Comp<FixturesComponent>(args.Target);
                    _transformSystem.Unanchor(args.Target); // Just in case.

                    _physics.SetBodyType(args.Target, BodyType.Dynamic, body: physics);
                    _physics.SetBodyStatus(args.Target, physics, BodyStatus.InAir);
                    _physics.WakeBody(args.Target, manager: fixtures, body: physics);

                    foreach (var fixture in fixtures.Fixtures.Values)
                    {
                        _physics.SetHard(args.Target, fixture, false, manager: fixtures);
                    }

                    _physics.SetLinearVelocity(args.Target, _random.NextVector2(8.0f, 8.0f), manager: fixtures, body: physics);
                    _physics.SetAngularVelocity(args.Target, MathF.PI * 12, manager: fixtures, body: physics);
                    _physics.SetLinearDamping(args.Target, physics, 0f);
                    _physics.SetAngularDamping(args.Target, physics, 0f);
                },
                Impact = LogImpact.Extreme,
                Message = string.Join(": ", yeetName, Loc.GetString("admin-smite-yeet-description"))
            };
            args.Verbs.Add(yeet);
        }

        if (TryComp<ActorComponent>(args.Target, out var actorComponent))
        {
            var ghostKickName = Loc.GetString("admin-smite-ghostkick-name").ToLowerInvariant();
            Verb ghostKick = new()
            {
                Text = ghostKickName,
                Act = () =>
                {
                    _ghostKickManager.DoDisconnect(actorComponent.PlayerSession.Channel, "Smitten.");
                },
                Impact = LogImpact.Extreme,
                Message = string.Join(": ", ghostKickName, Loc.GetString("admin-smite-ghostkick-description"))

            };
            args.Verbs.Add(ghostKick);
        }

        if (TryComp<InventoryComponent>(args.Target, out var inventory))
        {
            var nyanifyName = Loc.GetString("admin-smite-nyanify-name").ToLowerInvariant();
            Verb nyanify = new()
            {
                Text = nyanifyName,
                Act = () =>
                {
                    var ears = Spawn("ClothingHeadHatCatEars", Transform(args.Target).Coordinates);
                    EnsureComp<UnremoveableComponent>(ears);
                    _inventorySystem.TryUnequip(args.Target, "head", true, true, false, inventory);
                    _inventorySystem.TryEquip(args.Target, ears, "head", true, true, false, inventory);
                },
                Impact = LogImpact.Extreme,
                Message = string.Join(": ", nyanifyName, Loc.GetString("admin-smite-nyanify-description"))
            };
            args.Verbs.Add(nyanify);

            var maidenName = Loc.GetString("admin-smite-maid-name").ToLowerInvariant();
            Verb maiden = new()
            {
                Text = maidenName,
                Act = () =>
                {
                    _outfit.SetOutfit(args.Target, "JanitorMaidGear", (_, clothing) =>
                    {
                        if (HasComp<ClothingComponent>(clothing))
                            EnsureComp<UnremoveableComponent>(clothing);
                    });
                },
                Impact = LogImpact.Extreme,
                Message = string.Join(": ", maidenName, Loc.GetString("admin-smite-maid-description"))
            };
            args.Verbs.Add(maiden);
        }

        var dustName = Loc.GetString("admin-smite-dust-name").ToLowerInvariant();
        Verb dust = new()
        {
            Text = dustName,
            Act = () =>
            {
                QueueDel(args.Target);
                Spawn("Ash", Transform(args.Target).Coordinates);
                _popupSystem.PopupEntity(Loc.GetString("admin-smite-turned-ash-other", ("name", args.Target)), args.Target, PopupType.LargeCaution);
            },
            Impact = LogImpact.Extreme,
            Message = string.Join(": ", dustName, Loc.GetString("admin-smite-dust-description"))
        };
        args.Verbs.Add(dust);


        var lockerName = Loc.GetString("admin-smite-locker-stuff-name").ToLowerInvariant();
        Verb locker = new()
        {
            Text = lockerName,
            Act = () =>
            {
                var xform = Transform(args.Target);
                var locker = Spawn("ClosetMaintenance", xform.Coordinates);
                if (TryComp<EntityStorageComponent>(locker, out var storage))
                {
                    _entityStorageSystem.ToggleOpen(args.Target, locker, storage);
                    _entityStorageSystem.Insert(args.Target, locker, storage);
                    _entityStorageSystem.ToggleOpen(args.Target, locker, storage);
                }
                _weldableSystem.SetWeldedState(locker, true);
            },
            Impact = LogImpact.Extreme,
            Message = string.Join(": ", lockerName, Loc.GetString("admin-smite-locker-stuff-description"))
        };
        args.Verbs.Add(locker);

        var zoomInName = Loc.GetString("admin-smite-zoom-in-name").ToLowerInvariant();
        Verb zoomIn = new()
        {
            Text = zoomInName,
            Act = () =>
            {
                var eye = EnsureComp<ContentEyeComponent>(args.Target);
                _eyeSystem.SetZoom(args.Target, eye.TargetZoom * 0.2f, ignoreLimits: true);
            },
            Impact = LogImpact.Extreme,
            Message = string.Join(": ", zoomInName, Loc.GetString("admin-smite-zoom-in-description"))
        };
        args.Verbs.Add(zoomIn);

        var flipEyeName = Loc.GetString("admin-smite-flip-eye-name").ToLowerInvariant();
        Verb flipEye = new()
        {
            Text = flipEyeName,
            Act = () =>
            {
                var eye = EnsureComp<ContentEyeComponent>(args.Target);
                _eyeSystem.SetZoom(args.Target, eye.TargetZoom * -1, ignoreLimits: true);
            },
            Impact = LogImpact.Extreme,
            Message = string.Join(": ", flipEyeName, Loc.GetString("admin-smite-flip-eye-description"))
        };
        args.Verbs.Add(flipEye);

        var runWalkSwapName = Loc.GetString("admin-smite-run-walk-swap-name").ToLowerInvariant();
        Verb runWalkSwap = new()
        {
            Text = runWalkSwapName,
            Act = () =>
            {
                var movementSpeed = EnsureComp<MovementSpeedModifierComponent>(args.Target);
                (movementSpeed.BaseSprintSpeed, movementSpeed.BaseWalkSpeed) = (movementSpeed.BaseWalkSpeed, movementSpeed.BaseSprintSpeed);

                Dirty(args.Target, movementSpeed);

                _popupSystem.PopupEntity(Loc.GetString("admin-smite-run-walk-swap-prompt"), args.Target,
                    args.Target, PopupType.LargeCaution);
            },
            Impact = LogImpact.Extreme,
            Message = string.Join(": ", runWalkSwapName, Loc.GetString("admin-smite-run-walk-swap-description"))
        };
        args.Verbs.Add(runWalkSwap);

        //Bonk
        var superBonkLiteName = Loc.GetString("admin-smite-super-bonk-lite-name").ToLowerInvariant();
        Verb superBonkLite = new()
        {
            Text = superBonkLiteName,
            Act = () =>
            {
                _superBonkSystem.StartSuperBonk(args.Target, stopWhenDead: true);
            },
            Impact = LogImpact.Extreme,
            Message = string.Join(": ", superBonkLiteName, Loc.GetString("admin-smite-super-bonk-lite-description"))
        };
        args.Verbs.Add(superBonkLite);

        var superBonkName = Loc.GetString("admin-smite-super-bonk-name").ToLowerInvariant();
        Verb superBonk = new()
        {
            Text = superBonkName,
            Act = () =>
            {
                _superBonkSystem.StartSuperBonk(args.Target);
            },
            Impact = LogImpact.Extreme,
            Message = string.Join(": ", superBonkName, Loc.GetString("admin-smite-super-bonk-description"))
        };
        args.Verbs.Add(superBonk);

        var superslipName = Loc.GetString("admin-smite-super-slip-name").ToLowerInvariant();
        Verb superslip = new()
        {
            Text = superslipName,
            Act = () =>
            {
                var hadSlipComponent = EnsureComp(args.Target, out SlipperyComponent slipComponent);
                if (!hadSlipComponent)
                {
                    slipComponent.SlipData.SuperSlippery = true;
                    slipComponent.SlipData.StunTime = TimeSpan.FromSeconds(5);
                    slipComponent.SlipData.LaunchForwardsMultiplier = 20;
                }

                _slipperySystem.TrySlip(args.Target, slipComponent, args.Target, requiresContact: false);
                if (!hadSlipComponent)
                {
                    RemComp(args.Target, slipComponent);
                }
            },
            Impact = LogImpact.Extreme,
            Message = string.Join(": ", superslipName, Loc.GetString("admin-smite-super-slip-description"))
        };
        args.Verbs.Add(superslip);
    }
}
