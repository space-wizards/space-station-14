using Content.Shared.Administration.Components;
using Content.Shared.Atmos.Components;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.Cluwne;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.Electrocution;
using Content.Shared.Gravity;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Polymorph;
using Content.Shared.Popups;
using Content.Shared.Slippery;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Tools.Systems;
using Content.Shared.Verbs;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Shared.Administration;

public abstract partial class SharedAdminVerbSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly FixtureSystem _fixtureSystem = default!;
    [Dependency] private readonly MobThresholdSystem _mobThresholdSystem = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifierSystem = default!;
    [Dependency] private readonly SharedBodySystem _bodySystem = default!;
    [Dependency] private readonly SharedBloodstreamSystem _bloodstreamSystem = default!;
    [Dependency] private readonly SharedContentEyeSystem _eyeSystem = default!;
    [Dependency] private readonly SharedCreamPieSystem _creamPieSystem = default!;
    [Dependency] private readonly SharedElectrocutionSystem _electrocutionSystem = default!;
    [Dependency] private readonly SharedEntityStorageSystem _entityStorageSystem = default!;
    [Dependency] private readonly SlipperySystem _slipperySystem = default!;
    [Dependency] private readonly WeldableSystem _weldableSystem = default!;

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

        var explodeName = Loc.GetString("admin-smite-explode-name").ToLowerInvariant();
        Verb explode = new()
        {
            Text = explodeName,
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/smite.svg.192dpi.png")),
            Act = () => SmiteExplodeVerb(args.Target),
            Impact = LogImpact.Extreme,
            Message = string.Join(": ", explodeName, Loc.GetString("admin-smite-explode-description")) // we do this so the description tells admins the Text to run it via console.
        };
        args.Verbs.Add(explode);

        var chessName = Loc.GetString("admin-smite-chess-dimension-name").ToLowerInvariant();
        Verb chess = new()
        {
            Text = chessName,
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/Objects/Fun/Tabletop/chessboard.rsi"), "chessboard"),
            Act = () => SmiteChessVerb(args.Target),
            Impact = LogImpact.Extreme,
            Message = string.Join(": ", chessName, Loc.GetString("admin-smite-chess-dimension-description"))
        };
        args.Verbs.Add(chess);

        if (TryComp<FlammableComponent>(args.Target, out var flammable))
        {
            var flamesName = Loc.GetString("admin-smite-set-alight-name").ToLowerInvariant();
            Verb flames = new()
            {
                Text = flamesName,
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/Alerts/Fire/fire.png")),
                Act = () => SmiteSetAlightVerb(args.User, args.Target, flammable),
                Impact = LogImpact.Extreme,
                Message = string.Join(": ", flamesName, Loc.GetString("admin-smite-set-alight-description"))
            };
            args.Verbs.Add(flames);
        }

        var monkeyName = Loc.GetString("admin-smite-monkeyify-name").ToLowerInvariant();
        Verb monkey = new()
        {
            Text = monkeyName,
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/Mobs/Animals/monkey.rsi"), "monkey"),
            Act = () => PolymorphEntity(args.Target, "AdminMonkeySmite"),
            Impact = LogImpact.Extreme,
            Message = string.Join(": ", monkeyName, Loc.GetString("admin-smite-monkeyify-description"))
        };
        args.Verbs.Add(monkey);

        var disposalBinName = Loc.GetString("admin-smite-garbage-can-name").ToLowerInvariant();
        Verb disposalBin = new()
        {
            Text = disposalBinName,
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/Structures/Piping/disposal.rsi"), "disposal"),
            Act = () => PolymorphEntity(args.Target, "AdminDisposalsSmite"),
            Impact = LogImpact.Extreme,
            Message = string.Join(": ", disposalBinName, Loc.GetString("admin-smite-garbage-can-description"))
        };
        args.Verbs.Add(disposalBin);

        if (TryComp<DamageableComponent>(args.Target, out var damageable) && HasComp<MobStateComponent>(args.Target))
        {
            var hardElectrocuteName = Loc.GetString("admin-smite-electrocute-name").ToLowerInvariant();
            Verb hardElectrocute = new()
            {
                Text = hardElectrocuteName,
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/Clothing/Hands/Gloves/Color/yellow.rsi"), "icon"),
                Act = () =>
                {
                    int damageToDeal;
                    if (!_mobThresholdSystem.TryGetThresholdForState(args.Target, MobState.Critical, out var criticalThreshold))
                    {
                        // We can't crit them so try killing them.
                        if (!_mobThresholdSystem.TryGetThresholdForState(args.Target, MobState.Dead, out var deadThreshold))
                            return;// whelp.
                        damageToDeal = deadThreshold.Value.Int() - (int) damageable.TotalDamage;
                    }
                    else
                    {
                        damageToDeal = criticalThreshold.Value.Int() - (int) damageable.TotalDamage;
                    }

                    if (damageToDeal <= 0)
                        damageToDeal = 100; // murder time.

                    if (_inventorySystem.TryGetSlots(args.Target, out var slotDefinitions))
                    {
                        foreach (var slot in slotDefinitions)
                        {
                            if (!_inventorySystem.TryGetSlotEntity(args.Target, slot.Name, out var slotEnt))
                                continue;

                            RemComp<InsulatedComponent>(slotEnt.Value); // Fry the gloves.
                        }
                    }

                    _electrocutionSystem.TryDoElectrocution(args.Target,
                        null,
                        damageToDeal,
                        TimeSpan.FromSeconds(30),
                        refresh: true,
                        ignoreInsulation: true);
                },
                Impact = LogImpact.Extreme,
                Message = string.Join(": ", hardElectrocuteName, Loc.GetString("admin-smite-electrocute-description"))
            };
            args.Verbs.Add(hardElectrocute);
        }

        if (TryComp<CreamPiedComponent>(args.Target, out var creamPied))
        {
            var creamPieName = Loc.GetString("admin-smite-creampie-name").ToLowerInvariant();
            Verb creamPie = new()
            {
                Text = creamPieName,
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/Objects/Consumable/Food/Baked/pie.rsi"), "plain-slice"),
                Act = () => _creamPieSystem.SetCreamPied(args.Target, creamPied, true),
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
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/Fluids/tomato_splat.rsi"), "puddle-1"),
                Act = () =>
                {
                    _bloodstreamSystem.SpillAllSolutions((args.Target, bloodstream));
                    var xform = Transform(args.Target);
                    _popupSystem.PopupClient(Loc.GetString("admin-smite-remove-blood-self"),
                        args.Target,
                        args.Target,
                        PopupType.LargeCaution);
                    _popupSystem.PopupCoordinates(Loc.GetString("admin-smite-remove-blood-others", ("name", args.Target)),
                        xform.Coordinates,
                        Filter.PvsExcept(args.Target),
                        true,
                        PopupType.MediumCaution);
                },
                Impact = LogImpact.Extreme,
                Message = string.Join(": ", bloodRemovalName, Loc.GetString("admin-smite-remove-blood-description"))
            };
            args.Verbs.Add(bloodRemoval);
        }

        // I don't know what this comment means, but by golly, it stays.
        // bobby...
        if (TryComp<BodyComponent>(args.Target, out var body))
        {
            var vomitOrgansName = Loc.GetString("admin-smite-vomit-organs-name").ToLowerInvariant();
            Verb vomitOrgans = new()
            {
                Text = vomitOrgansName,
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Rsi(new("/Textures/Fluids/vomit_toxin.rsi"), "vomit_toxin-1"),
                Act = () => SmiteVomitOrgansVerb(args.Target, body),
                Impact = LogImpact.Extreme,
                Message = string.Join(": ", vomitOrgansName, Loc.GetString("admin-smite-vomit-organs-description"))
            };
            args.Verbs.Add(vomitOrgans);

            var handsRemovalName = Loc.GetString("admin-smite-remove-hands-name").ToLowerInvariant();
            Verb handsRemoval = new()
            {
                Text = handsRemovalName,
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/AdminActions/remove-hands.png")),
                Act = () =>
                {
                    var baseXform = Transform(args.Target);
                    foreach (var part in _bodySystem.GetBodyChildrenOfType(args.Target, BodyPartType.Hand))
                    {
                        _transformSystem.AttachToGridOrMap(part.Id);
                    }

                    _popupSystem.PopupClient(Loc.GetString("admin-smite-remove-hands-self"),
                        args.Target,
                        args.Target,
                        PopupType.LargeCaution);
                    _popupSystem.PopupCoordinates(
                        Loc.GetString("admin-smite-remove-hands-other", ("name", args.Target)),
                        baseXform.Coordinates,
                        Filter.PvsExcept(args.Target),
                        true,
                        PopupType.Medium);
                },
                Impact = LogImpact.Extreme,
                Message = string.Join(": ", handsRemovalName, Loc.GetString("admin-smite-remove-hands-description"))
            };
            args.Verbs.Add(handsRemoval);

            var handRemovalName = Loc.GetString("admin-smite-remove-hand-name").ToLowerInvariant();
            Verb handRemoval = new()
            {
                Text = handRemovalName,
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/AdminActions/remove-hand.png")),
                Act = () =>
                {
                    var baseXform = Transform(args.Target);
                    foreach (var part in _bodySystem.GetBodyChildrenOfType(args.Target, BodyPartType.Hand, body))
                    {
                        _transformSystem.AttachToGridOrMap(part.Id);
                        break;
                    }

                    _popupSystem.PopupClient(Loc.GetString("admin-smite-remove-hands-self"),
                        args.Target,
                        args.Target,
                        PopupType.LargeCaution);
                    _popupSystem.PopupCoordinates(
                        Loc.GetString("admin-smite-remove-hands-other", ("name", args.Target)),
                        baseXform.Coordinates,
                        Filter.PvsExcept(args.Target),
                        true,
                        PopupType.Medium);
                },
                Impact = LogImpact.Extreme,
                Message = string.Join(": ", handRemovalName, Loc.GetString("admin-smite-remove-hand-description"))
            };
            args.Verbs.Add(handRemoval);

            var stomachRemovalName = Loc.GetString("admin-smite-stomach-removal-name").ToLowerInvariant();
            Verb stomachRemoval = new()
            {
                Text = stomachRemovalName,
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/Mobs/Species/Human/organs.rsi"), "stomach"),
                Act = () =>
                {
                    foreach (var entity in _bodySystem.GetBodyOrganEntityComps<StomachComponent>((args.Target, body)))
                    {
                        QueueDel(entity.Owner);
                    }

                    _popupSystem.PopupClient(Loc.GetString("admin-smite-stomach-removal-self"),
                        args.Target,
                        args.Target,
                        PopupType.LargeCaution);
                },
                Impact = LogImpact.Extreme,
                Message = string.Join(": ",
                    stomachRemovalName,
                    Loc.GetString("admin-smite-stomach-removal-description"))
            };
            args.Verbs.Add(stomachRemoval);

            var lungRemovalName = Loc.GetString("admin-smite-lung-removal-name").ToLowerInvariant();
            Verb lungRemoval = new()
            {
                Text = lungRemovalName,
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/Mobs/Species/Human/organs.rsi"), "lung-r"),
                Act = () => SmiteRemoveLungsVerb(args.Target, body),
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
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/Objects/Fun/Balls/basketball.rsi"), "icon"),
                Act = () =>
                {
                    var xform = Transform(args.Target);
                    var fixtures = Comp<FixturesComponent>(args.Target);
                    _transformSystem.Unanchor(args.Target, xform); // Just in case.
                    _physicsSystem.SetBodyType(args.Target, BodyType.Dynamic, manager: fixtures, body: physics);
                    _physicsSystem.SetBodyStatus(args.Target, physics, BodyStatus.InAir);
                    _physicsSystem.WakeBody(args.Target, manager: fixtures, body: physics);

                    foreach (var fixture in fixtures.Fixtures.Values)
                    {
                        if (!fixture.Hard)
                            continue;

                        _physicsSystem.SetRestitution(args.Target, fixture, 1.1f, false, fixtures);
                    }

                    _fixtureSystem.FixtureUpdate(args.Target, manager: fixtures, body: physics);

                    _physicsSystem.SetLinearVelocity(args.Target, _random.NextVector2(1.5f, 1.5f), manager: fixtures, body: physics);
                    _physicsSystem.SetAngularVelocity(args.Target, MathF.PI * 12, manager: fixtures, body: physics);
                    _physicsSystem.SetLinearDamping(args.Target, physics, 0f);
                    _physicsSystem.SetAngularDamping(args.Target, physics, 0f);
                },
                Impact = LogImpact.Extreme,
                Message = string.Join(": ", pinballName, Loc.GetString("admin-smite-pinball-description"))
            };
            args.Verbs.Add(pinball);

            var yeetName = Loc.GetString("admin-smite-yeet-name").ToLowerInvariant();
            Verb yeet = new()
            {
                Text = yeetName,
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/eject.svg.192dpi.png")),
                Act = () =>
                {
                    var fixtures = Comp<FixturesComponent>(args.Target);
                    _transformSystem.Unanchor(args.Target); // Just in case.

                    _physicsSystem.SetBodyType(args.Target, BodyType.Dynamic, body: physics);
                    _physicsSystem.SetBodyStatus(args.Target, physics, BodyStatus.InAir);
                    _physicsSystem.WakeBody(args.Target, manager: fixtures, body: physics);

                    foreach (var fixture in fixtures.Fixtures.Values)
                    {
                        _physicsSystem.SetHard(args.Target, fixture, false, manager: fixtures);
                    }

                    _physicsSystem.SetLinearVelocity(args.Target, _random.NextVector2(8.0f, 8.0f), manager: fixtures, body: physics);
                    _physicsSystem.SetAngularVelocity(args.Target, MathF.PI * 12, manager: fixtures, body: physics);
                    _physicsSystem.SetLinearDamping(args.Target, physics, 0f);
                    _physicsSystem.SetAngularDamping(args.Target, physics, 0f);
                },
                Impact = LogImpact.Extreme,
                Message = string.Join(": ", yeetName, Loc.GetString("admin-smite-yeet-description"))
            };
            args.Verbs.Add(yeet);
        }

        var breadName = Loc.GetString("admin-smite-become-bread-name").ToLowerInvariant(); // Will I get cancelled for breadName-ing you?
        Verb bread = new()
        {
            Text = breadName,
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/Objects/Consumable/Food/Baked/bread.rsi"), "plain"),
            Act = () => PolymorphEntity(args.Target, "AdminBreadSmite"),
            Impact = LogImpact.Extreme,
            Message = string.Join(": ", breadName, Loc.GetString("admin-smite-become-bread-description"))
        };
        args.Verbs.Add(bread);

        var mouseName = Loc.GetString("admin-smite-become-mouse-name").ToLowerInvariant();
        Verb mouse = new()
        {
            Text = mouseName,
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/Mobs/Animals/mouse.rsi"), "icon-0"),
            Act = () => PolymorphEntity(args.Target, "AdminMouseSmite"),
            Impact = LogImpact.Extreme,
            Message = string.Join(": ", mouseName, Loc.GetString("admin-smite-become-mouse-description"))
        };
        args.Verbs.Add(mouse);

        if (TryComp<ActorComponent>(args.Target, out var actorComponent))
        {
            var ghostKickName = Loc.GetString("admin-smite-ghostkick-name").ToLowerInvariant();
            Verb ghostKick = new()
            {
                Text = ghostKickName,
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/gavel.svg.192dpi.png")),
                Act = () => SmiteGhostKickVerb(actorComponent),
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
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/Clothing/Head/Hats/catears.rsi"), "icon"),
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

            var killSignName = Loc.GetString("admin-smite-kill-sign-name").ToLowerInvariant();
            Verb killSign = new()
            {
                Text = killSignName,
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/Objects/Misc/killsign.rsi"), "icon"),
                Act = () => SmiteKillSignVerb(args.Target),
                Impact = LogImpact.Extreme,
                Message = string.Join(": ", killSignName, Loc.GetString("admin-smite-kill-sign-description"))
            };
            args.Verbs.Add(killSign);

            var cluwneName = Loc.GetString("admin-smite-cluwne-name").ToLowerInvariant();
            Verb cluwne = new()
            {
                Text = cluwneName,
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/Clothing/Mask/cluwne.rsi"), "icon"),
                Act = () => EnsureComp<CluwneComponent>(args.Target),
                Impact = LogImpact.Extreme,
                Message = string.Join(": ", cluwneName, Loc.GetString("admin-smite-cluwne-description"))
            };
            args.Verbs.Add(cluwne);

            var maidenName = Loc.GetString("admin-smite-maid-name").ToLowerInvariant();
            Verb maiden = new()
            {
                Text = maidenName,
                Category = VerbCategory.Smite,
                Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/Clothing/Uniforms/Jumpskirt/janimaid.rsi"), "icon"),
                Act = () => SmiteMaidenVerb(args.Target),
                Impact = LogImpact.Extreme,
                Message = string.Join(": ", maidenName, Loc.GetString("admin-smite-maid-description"))
            };
            args.Verbs.Add(maiden);
        }

        var angerPointingArrowsName = Loc.GetString("admin-smite-anger-pointing-arrows-name").ToLowerInvariant();
        Verb angerPointingArrows = new()
        {
            Text = angerPointingArrowsName,
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/Interface/Misc/pointing.rsi"), "pointing"),
            Act = () => SmiteAngerPointingArrowsVerb(args.Target),
            Impact = LogImpact.Extreme,
            Message = string.Join(": ", angerPointingArrowsName, Loc.GetString("admin-smite-anger-pointing-arrows-description"))
        };
        args.Verbs.Add(angerPointingArrows);

        var dustName = Loc.GetString("admin-smite-dust-name").ToLowerInvariant();
        Verb dust = new()
        {
            Text = dustName,
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/Objects/Materials/materials.rsi"), "ash"),
            Act = () =>
            {
                QueueDel(args.Target);
                Spawn("Ash", Transform(args.Target).Coordinates);
                _popupSystem.PopupClient(Loc.GetString("admin-smite-turned-ash-other", ("name", args.Target)), args.Target, PopupType.LargeCaution);
            },
            Impact = LogImpact.Extreme,
            Message = string.Join(": ", dustName, Loc.GetString("admin-smite-dust-description"))
        };
        args.Verbs.Add(dust);

        var youtubeVideoSimulationName = Loc.GetString("admin-smite-buffering-name").ToLowerInvariant();
        Verb youtubeVideoSimulation = new()
        {
            Text = youtubeVideoSimulationName,
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/Misc/buffering_smite_icon.png")),
            Act = () => SmiteYoutubeSimulationVerb(args.Target),
            Impact = LogImpact.Extreme,
            Message = string.Join(": ", youtubeVideoSimulationName, Loc.GetString("admin-smite-buffering-description"))
        };
        args.Verbs.Add(youtubeVideoSimulation);

        var noGravityName = Loc.GetString("admin-smite-remove-gravity-name").ToLowerInvariant();
        Verb noGravity = new()
        {
            Text = noGravityName,
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/Structures/Machines/gravity_generator.rsi"), "off"),
            Act = () =>
            {
                var grav = EnsureComp<MovementIgnoreGravityComponent>(args.Target);
                grav.Weightless = true;

                Dirty(args.Target, grav);

                EnsureComp<GravityAffectedComponent>(args.Target, out var weightless);
                weightless.Weightless = true;

                Dirty(args.Target, weightless);
            },
            Impact = LogImpact.Extreme,
            Message = string.Join(": ", noGravityName, Loc.GetString("admin-smite-remove-gravity-description"))
        };
        args.Verbs.Add(noGravity);

        var reptilianName = Loc.GetString("admin-smite-reptilian-species-swap-name").ToLowerInvariant();
        Verb reptilian = new()
        {
            Text = reptilianName,
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/Objects/Fun/Plushies/lizard.rsi"), "icon"),
            Act = () => PolymorphEntity(args.Target, "AdminLizardSmite"),
            Impact = LogImpact.Extreme,
            Message = string.Join(": ", reptilianName, Loc.GetString("admin-smite-reptilian-species-swap-description"))
        };
        args.Verbs.Add(reptilian);

        var lockerName = Loc.GetString("admin-smite-locker-stuff-name").ToLowerInvariant();
        Verb locker = new()
        {
            Text = lockerName,
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/Structures/Storage/closet.rsi"), "generic"),
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

        var headstandName = Loc.GetString("admin-smite-headstand-name").ToLowerInvariant();
        Verb headstand = new()
        {
            Text = headstandName,
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/refresh.svg.192dpi.png")),
            Act = () => EnsureComp<HeadstandComponent>(args.Target),
            Impact = LogImpact.Extreme,
            Message = string.Join(": ", headstandName, Loc.GetString("admin-smite-headstand-description"))
        };
        args.Verbs.Add(headstand);

        var zoomInName = Loc.GetString("admin-smite-zoom-in-name").ToLowerInvariant();
        Verb zoomIn = new()
        {
            Text = zoomInName,
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/AdminActions/zoom.png")),
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
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/AdminActions/flip.png")),
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
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/AdminActions/run-walk-swap.png")),
            Act = () =>
            {
                var movementSpeed = EnsureComp<MovementSpeedModifierComponent>(args.Target);
                (movementSpeed.BaseSprintSpeed, movementSpeed.BaseWalkSpeed) = (movementSpeed.BaseWalkSpeed, movementSpeed.BaseSprintSpeed);

                Dirty(args.Target, movementSpeed);

                _popupSystem.PopupClient(Loc.GetString("admin-smite-run-walk-swap-prompt"),
                    args.Target,
                    args.Target,
                    PopupType.LargeCaution);
            },
            Impact = LogImpact.Extreme,
            Message = string.Join(": ", runWalkSwapName, Loc.GetString("admin-smite-run-walk-swap-description"))
        };
        args.Verbs.Add(runWalkSwap);

        var backwardsAccentName = Loc.GetString("admin-smite-speak-backwards-name").ToLowerInvariant();
        Verb backwardsAccent = new()
        {
            Text = backwardsAccentName,
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/AdminActions/help-backwards.png")),
            Act = () => SmiteBackwardsAccentVerb(args.Target),
            Impact = LogImpact.Extreme,
            Message = string.Join(": ", backwardsAccentName, Loc.GetString("admin-smite-speak-backwards-description"))
        };
        args.Verbs.Add(backwardsAccent);

        var disarmProneName = Loc.GetString("admin-smite-disarm-prone-name").ToLowerInvariant();
        Verb disarmProne = new()
        {
            Text = disarmProneName,
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/Actions/disarm.png")),
            Act = () => EnsureComp<DisarmProneComponent>(args.Target),
            Impact = LogImpact.Extreme,
            Message = string.Join(": ", disarmProneName, Loc.GetString("admin-smite-disarm-prone-description"))
        };
        args.Verbs.Add(disarmProne);

        var superSpeedName = Loc.GetString("admin-smite-super-speed-name").ToLowerInvariant();
        Verb superSpeed = new()
        {
            Text = superSpeedName,
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/AdminActions/super_speed.png")),
            Act = () =>
            {
                var movementSpeed = EnsureComp<MovementSpeedModifierComponent>(args.Target);
                _movementSpeedModifierSystem.ChangeBaseSpeed(args.Target, 400, 8000, 40, movementSpeed);

                _popupSystem.PopupClient(Loc.GetString("admin-smite-super-speed-prompt"),
                    args.Target,
                    args.Target,
                    PopupType.LargeCaution);
            },
            Impact = LogImpact.Extreme,
            Message = string.Join(": ", superSpeedName, Loc.GetString("admin-smite-super-speed-description"))
        };
        args.Verbs.Add(superSpeed);

        //Bonk
        var superBonkLiteName = Loc.GetString("admin-smite-super-bonk-lite-name").ToLowerInvariant();
        Verb superBonkLite = new()
        {
            Text = superBonkLiteName,
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Rsi(new("Structures/Furniture/Tables/glass.rsi"), "full"),
            Act = () => SmiteSuperBonkLiteVerb(args.Target),
            Impact = LogImpact.Extreme,
            Message = string.Join(": ", superBonkLiteName, Loc.GetString("admin-smite-super-bonk-lite-description"))
        };
        args.Verbs.Add(superBonkLite);

        var superBonkName = Loc.GetString("admin-smite-super-bonk-name").ToLowerInvariant();
        Verb superBonk = new()
        {
            Text = superBonkName,
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Rsi(new("Structures/Furniture/Tables/generic.rsi"), "full"),
            Act = () => SmiteSuperBonkVerb(args.Target),
            Impact = LogImpact.Extreme,
            Message = string.Join(": ", superBonkName, Loc.GetString("admin-smite-super-bonk-description"))
        };
        args.Verbs.Add(superBonk);

        var superslipName = Loc.GetString("admin-smite-super-slip-name").ToLowerInvariant();
        Verb superslip = new()
        {
            Text = superslipName,
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Rsi(new("Objects/Specific/Janitorial/soap.rsi"), "omega-4"),
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
                    RemComp(args.Target, slipComponent);
            },
            Impact = LogImpact.Extreme,
            Message = string.Join(": ", superslipName, Loc.GetString("admin-smite-super-slip-description"))
        };
        args.Verbs.Add(superslip);

        var omniaccentName = Loc.GetString("admin-smite-omni-accent-name").ToLowerInvariant();
        Verb omniaccent = new()
        {
            Text = omniaccentName,
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Rsi(new("Interface/Actions/voice-mask.rsi"), "icon"),
            Act = () => SmiteOmniAccentVerb(args.Target),
            Impact = LogImpact.Extreme,
            Message = string.Join(": ", omniaccentName, Loc.GetString("admin-smite-omni-accent-description"))
        };
        args.Verbs.Add(omniaccent);

        var crawlerName = Loc.GetString("admin-smite-crawler-name").ToLowerInvariant();
        Verb crawler = new()
        {
            Text = crawlerName,
            Category = VerbCategory.Smite,
            Icon = new SpriteSpecifier.Rsi(new("Mobs/Animals/snake.rsi"), "icon"),
            Act = () => EnsureComp<WormComponent>(args.Target),
            Impact = LogImpact.Extreme,
            Message = string.Join(": ", crawlerName, Loc.GetString("admin-smite-crawler-description"))
        };
        args.Verbs.Add(crawler);
    }

    protected virtual void PolymorphEntity(EntityUid uid, ProtoId<PolymorphPrototype> protoId)
    {
    }

    protected virtual void SmiteExplodeVerb(EntityUid target)
    {
    }

    protected virtual void SmiteChessVerb(EntityUid target)
    {
    }

    protected virtual void SmiteSetAlightVerb(EntityUid user, EntityUid target, FlammableComponent flammable)
    {
    }

    protected virtual void SmiteVomitOrgansVerb(EntityUid target, BodyComponent body)
    {
    }

    protected virtual void SmiteRemoveLungsVerb(EntityUid target, BodyComponent body)
    {
    }

    protected virtual void SmiteGhostKickVerb(ActorComponent actor)
    {
    }

    protected virtual void SmiteKillSignVerb(EntityUid target)
    {
    }

    protected virtual void SmiteMaidenVerb(EntityUid target)
    {
    }

    protected virtual void SmiteAngerPointingArrowsVerb(EntityUid target)
    {
    }

    protected virtual void SmiteYoutubeSimulationVerb(EntityUid target)
    {
    }

    protected virtual void SmiteBackwardsAccentVerb(EntityUid target)
    {
    }

    protected virtual void SmiteSuperBonkLiteVerb(EntityUid target)
    {
    }

    protected virtual void SmiteSuperBonkVerb(EntityUid target)
    {
    }

    protected virtual void SmiteOmniAccentVerb(EntityUid target)
    {
    }
}
