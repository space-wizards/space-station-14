using Content.Server.Antag;
using Content.Server.Antag.Components;
using Content.Server.DeadSpace.Traitor;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Zombies;
using Content.Shared.Administration;
using Content.Server.Clothing.Systems;
using Content.Shared.Database;
using Content.Shared.Humanoid;
using Content.Shared.Mind.Components;
using Content.Shared.Roles;
using Content.Shared.Verbs;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Content.Shared.DeadSpace.Events.Roles.Components;
using Content.Shared.DeadSpace.Renegade.Roles;
using Content.Shared.Roles.Components;
using Content.Shared.DeadSpace.Demons.Shadowling; //DS14
using Content.Server.DeadSpace.Hooligan.Components; //DS14
using Content.Shared.GameTicking.Components;
using Content.Server.GameTicking.Rules;

namespace Content.Server.Administration.Systems;

public sealed partial class AdminVerbSystem
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly ZombieSystem _zombie = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly OutfitSystem _outfit = default!;
    [Dependency] private readonly TraitorUltraRuleSystem _traitorUltra = default!; // DS14
    [Dependency] private readonly UnitologyRuleSystem _unitologyRule = default!; // DS14

    private static readonly EntProtoId DefaultTraitorRule = "Traitor";
    private static readonly EntProtoId DefaultInitialInfectedRule = "Zombie";
    private static readonly EntProtoId DefaultNukeOpRule = "LoneOpsSpawn";
    private static readonly EntProtoId NukeopsRule = "Nukeops"; // DS14
    private static readonly EntProtoId DefaultRevsRule = "Revolutionary";
    private static readonly EntProtoId DefaultThiefRule = "Thief";
    private static readonly EntProtoId DefaultChangelingRule = "Changeling";
    private static readonly EntProtoId ParadoxCloneRuleId = "ParadoxCloneSpawn";
    private static readonly EntProtoId DefaultWizardRule = "Wizard";
    private static readonly EntProtoId DefaultNinjaRule = "NinjaSpawn";
    private static readonly EntProtoId DefaultSpiderTerrorRule = "SpiderTerror"; // DS14
    private static readonly EntProtoId DragonSpawnRule = "DragonSpawn"; //  DS14
    private static readonly EntProtoId RenegadeRule = "RenegadeSpawn"; // DS14
    private static readonly EntProtoId DefaultHooliganRule = "Hooligan"; // DS14
    private static readonly ProtoId<StartingGearPrototype> PirateGearId = "PirateGear";

    // All antag verbs have names so invokeverb works.
    private void AddAntagVerbs(GetVerbsEvent<Verb> args)
    {
        if (!TryComp<ActorComponent>(args.User, out var actor))
            return;

        var player = actor.PlayerSession;

        if (!_adminManager.HasAdminFlag(player, AdminFlags.Fun))
            return;

        if (!HasComp<MindContainerComponent>(args.Target) || !TryComp<ActorComponent>(args.Target, out var targetActor))
            return;

        var targetPlayer = targetActor.PlayerSession;

        var traitorName = Loc.GetString("admin-verb-text-make-traitor");
        Verb traitor = new()
        {
            Text = traitorName,
            Category = VerbCategory.Antag,
            Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/Interface/Misc/job_icons.rsi"), "Syndicate"),
            Act = () =>
            {
                // DS14-start
                if (TryGetTraitorUltraRule(out var traitorUltraRule))
                {
                    _traitorUltra.MakeAdminTraitorUltra(traitorUltraRule, targetPlayer, traitorUltraRule.Comp2.Definitions[^1]);
                    return;
                }
                // DS14-end

                _antag.ForceMakeAntag<TraitorRuleComponent>(targetPlayer, DefaultTraitorRule);
            },
            Impact = LogImpact.High,
            Message = string.Join(": ", traitorName, Loc.GetString("admin-verb-make-traitor")),
        };
        args.Verbs.Add(traitor);

        var initialInfectedName = Loc.GetString("admin-verb-text-make-initial-infected");
        Verb initialInfected = new()
        {
            Text = initialInfectedName,
            Category = VerbCategory.Antag,
            Icon = new SpriteSpecifier.Rsi(new("/Textures/Interface/Misc/job_icons.rsi"), "InitialInfected"),
            Act = () =>
            {
                _antag.ForceMakeAntag<ZombieRuleComponent>(targetPlayer, DefaultInitialInfectedRule);
            },
            Impact = LogImpact.High,
            Message = string.Join(": ", initialInfectedName, Loc.GetString("admin-verb-make-initial-infected")),
        };
        args.Verbs.Add(initialInfected);

        // DS14-start
        var blobName = Loc.GetString("admin-verb-text-make-blob");
        Verb blob = new()
        {
            Text = blobName,
            Category = VerbCategory.Antag,
            Icon = new SpriteSpecifier.Rsi(new("/Textures/_Backmen/Interface/Actions/blob.rsi"), "blobFactory"),
            Act = () =>
            {
                _antag.TrackGrantedComponent<Shared.Backmen.Blob.Components.BlobCarrierComponent>(args.Target);
                EnsureComp<Shared.Backmen.Blob.Components.BlobCarrierComponent>(args.Target).HasMind = HasComp<ActorComponent>(args.Target);
            },
            Impact = LogImpact.High,
            Message = string.Join(": ", blobName, Loc.GetString("admin-verb-make-blob")),
        };
        args.Verbs.Add(blob);
        // DS14-end

        var zombieName = Loc.GetString("admin-verb-text-make-zombie");
        Verb zombie = new()
        {
            Text = zombieName,
            Category = VerbCategory.Antag,
            Icon = new SpriteSpecifier.Rsi(new("/Textures/Interface/Misc/job_icons.rsi"), "Zombie"),
            Act = () =>
            {
                _zombie.ZombifyEntity(args.Target);
            },
            Impact = LogImpact.High,
            Message = string.Join(": ", zombieName, Loc.GetString("admin-verb-make-zombie")),
        };
        args.Verbs.Add(zombie);

        var nukeOpName = Loc.GetString("admin-verb-text-make-nuclear-operative");
        Verb nukeOp = new()
        {
            Text = nukeOpName,
            Category = VerbCategory.Antag,
            Icon = new SpriteSpecifier.Rsi(new("/Textures/Clothing/Head/Hardsuits/syndicate.rsi"), "icon"),
            Act = () =>
            {
                // DS14-start
                if (_gameTicker.IsGameRuleActive(NukeopsRule))
                {
                    var rule = _antag.ForceGetGameRuleEnt<NukeopsRuleComponent>(NukeopsRule);
                    _antag.MakeAntag(rule, targetPlayer, rule.Comp.Definitions[^1]);
                    return;
                }
                // DS14-end

                _antag.ForceMakeAntag<NukeopsRuleComponent>(targetPlayer, DefaultNukeOpRule, forceNewRule: true);
            },
            Impact = LogImpact.High,
            Message = string.Join(": ", nukeOpName, Loc.GetString("admin-verb-make-nuclear-operative")),
        };
        args.Verbs.Add(nukeOp);

        // DS14-start

        var dragonName = Loc.GetString("admin-verb-text-make-dragon");
        Verb dragon = new()
        {
            Text = dragonName,
            Category = VerbCategory.Antag,
            Icon = new SpriteSpecifier.Rsi(new("Objects/Weapons/Guns/Projectiles/magic.rsi"), "fireball"),
            Act = () =>
            {
                _antag.ForceMakeAntag<DragonRuleComponent>(targetPlayer, DragonSpawnRule);
            },
            Impact = LogImpact.High,
            Message = string.Join(": ", dragonName, Loc.GetString("admin-verb-make-dragon")),
        };
        args.Verbs.Add(dragon);

        var shadowlingName = Loc.GetString("admin-verb-text-make-shadowling");
        Verb shadowling = new()
        {
            Text = shadowlingName,
            Category = VerbCategory.Antag,
            Icon = new SpriteSpecifier.Rsi(new("/Textures/_DeadSpace/Interface/Misc/antag_icons.rsi"), "ShadowlingIcon2"),
            Act = () =>
            {
                if (targetPlayer.AttachedEntity is not { } target) return;
                _antag.ForceMakeAntag<ShadowlingRuleComponent>(targetPlayer, "ShadowlingRule");
                if (!HasComp<ShadowlingRevealComponent>(target))
                {
                    EnsureComp<ShadowlingRevealComponent>(target);
                    _antag.TrackGrantedComponent<ShadowlingRevealComponent>(target);
                }
            },
            Impact = LogImpact.High,
            Message = string.Join(": ", shadowlingName, "Сделать скрытым тенеморфом"),
        };
        args.Verbs.Add(shadowling);

        var renegadeName = Loc.GetString("admin-verb-text-make-renegade");
        Verb renegade = new()
        {
            Text = renegadeName,
            Category = VerbCategory.Antag,
            Icon = new SpriteSpecifier.Rsi(new("_DeadSpace/Renegade/bondrewd_helm.rsi"), "icon"),
            Act = () =>
            {
                _antag.ForceMakeAntag<RenegadeRoleComponent>(targetPlayer, RenegadeRule);
            },
            Impact = LogImpact.High,
            Message = string.Join(": ", renegadeName, Loc.GetString("admin-verb-make-renegade")),
        };
        args.Verbs.Add(renegade);

        //DS14-end

        var pirateName = Loc.GetString("admin-verb-text-make-pirate");
        Verb pirate = new()
        {
            Text = pirateName,
            Category = VerbCategory.Antag,
            Icon = new SpriteSpecifier.Rsi(new("/Textures/Clothing/Head/Hats/pirate.rsi"), "icon"),
            Act = () =>
            {
                // pirates just get an outfit because they don't really have logic associated with them
                _outfit.SetOutfit(args.Target, PirateGearId);
            },
            Impact = LogImpact.High,
            Message = string.Join(": ", pirateName, Loc.GetString("admin-verb-make-pirate")),
        };
        args.Verbs.Add(pirate);

        var headRevName = Loc.GetString("admin-verb-text-make-head-rev");
        Verb headRev = new()
        {
            Text = headRevName,
            Category = VerbCategory.Antag,
            Icon = new SpriteSpecifier.Rsi(new("/Textures/Interface/Misc/job_icons.rsi"), "HeadRevolutionary"),
            Act = () =>
            {
                _antag.ForceMakeAntag<RevolutionaryRuleComponent>(targetPlayer, DefaultRevsRule);
            },
            Impact = LogImpact.High,
            Message = string.Join(": ", headRevName, Loc.GetString("admin-verb-make-head-rev")),
        };
        args.Verbs.Add(headRev);

        // DS14-start
        var uniName = Loc.GetString("admin-verb-text-make-unitolog");
        Verb uni = new()
        {
            Text = uniName,
            Category = VerbCategory.Antag,
            Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/_DeadSpace/Interface/Misc/antag_icons.rsi"), "Unitology"),
            Act = () =>
            {
                _unitologyRule.TryGrantUnitologyRole(args.Target, UnitologyRuleSystem.RegularUnitologyAntagRole, targetPlayer);
            },
            Impact = LogImpact.High,
            Message = string.Join(": ", uniName, Loc.GetString("admin-verb-make-unitolog")),
        };
        args.Verbs.Add(uni);

        var spiderTerrorName = Loc.GetString("admin-verb-text-make-spider-terror");
        Verb spiderTerror = new()
        {
            Text = spiderTerrorName,
            Category = VerbCategory.Antag,
            Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/_DeadSpace/Interface/Misc/antag_icons.rsi"), "Egg"),
            Act = () =>
            {
                _antag.ForceMakeAntag<SpiderTerrorRuleComponent>(targetPlayer, DefaultSpiderTerrorRule);
            },
            Impact = LogImpact.High,
            Message = string.Join(": ", spiderTerrorName, Loc.GetString("admin-verb-make-spider-terror")),
        };
        args.Verbs.Add(spiderTerror);
        // DS14-end

        var thiefName = Loc.GetString("admin-verb-text-make-thief");
        Verb thief = new()
        {
            Text = thiefName,
            Category = VerbCategory.Antag,
            Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/Clothing/Hands/Gloves/Color/black.rsi"), "icon"),
            Act = () =>
            {
                _antag.ForceMakeAntag<ThiefRuleComponent>(targetPlayer, DefaultThiefRule);
            },
            Impact = LogImpact.High,
            Message = string.Join(": ", thiefName, Loc.GetString("admin-verb-make-thief")),
        };
        args.Verbs.Add(thief);

        // DS14-start
        var hooliganName = Loc.GetString("admin-verb-text-make-hooligan");
        Verb hooligan = new()
        {
            Text = hooliganName,
            Category = VerbCategory.Antag,
            Icon = new SpriteSpecifier.Rsi(new ResPath("Objects/Weapons/Melee/baseball_bat.rsi"), "icon"),
            Act = () =>
            {
                _antag.ForceMakeAntag<HooliganRuleComponent>(targetPlayer, DefaultHooliganRule);
            },
            Impact = LogImpact.High,
            Message = string.Join(": ", hooliganName, Loc.GetString("admin-verb-make-hooligan")),
        };
        args.Verbs.Add(hooligan);
        // DS14-end

        var changelingName = Loc.GetString("admin-verb-text-make-changeling");
        Verb changeling = new()
        {
            Text = changelingName,
            Category = VerbCategory.Antag,
            Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/Objects/Weapons/Melee/armblade.rsi"), "icon"),
            Act = () =>
            {
                _antag.ForceMakeAntag<ChangelingRuleComponent>(targetPlayer, DefaultChangelingRule);
            },
            Impact = LogImpact.High,
            Message = string.Join(": ", changelingName, Loc.GetString("admin-verb-make-changeling")),
        };
        args.Verbs.Add(changeling);

        var paradoxCloneName = Loc.GetString("admin-verb-text-make-paradox-clone");
        Verb paradox = new()
        {
            Text = paradoxCloneName,
            Category = VerbCategory.Antag,
            Icon = new SpriteSpecifier.Rsi(new("/Textures/Interface/Misc/job_icons.rsi"), "ParadoxClone"),
            Act = () =>
            {
                var ruleEnt = _gameTicker.AddGameRule(ParadoxCloneRuleId);

                if (!TryComp<ParadoxCloneRuleComponent>(ruleEnt, out var paradoxCloneRuleComp))
                    return;

                paradoxCloneRuleComp.OriginalBody = args.Target; // override the target player

                _gameTicker.StartGameRule(ruleEnt);
            },
            Impact = LogImpact.High,
            Message = string.Join(": ", paradoxCloneName, Loc.GetString("admin-verb-make-paradox-clone")),
        };

        var wizardName = Loc.GetString("admin-verb-text-make-wizard");
        Verb wizard = new()
        {
            Text = wizardName,
            Category = VerbCategory.Antag,
            Icon = new SpriteSpecifier.Rsi(new("/Textures/Interface/Misc/job_icons.rsi"), "Wizard"),
            Act = () =>
            {
                // Wizard has no rule components as of writing, but I gotta put something here to satisfy the machine so just make it wizard mind rule :)
                _antag.ForceMakeAntag<WizardRoleComponent>(targetPlayer, DefaultWizardRule);
            },
            Impact = LogImpact.High,
            Message = string.Join(": ", wizardName, Loc.GetString("admin-verb-make-wizard")),
        };
        args.Verbs.Add(wizard);

        var ninjaName = Loc.GetString("admin-verb-text-make-space-ninja");
        Verb ninja = new()
        {
            Text = ninjaName,
            Category = VerbCategory.Antag,
            Icon = new SpriteSpecifier.Rsi(new("/Textures/Objects/Weapons/Melee/energykatana.rsi"), "icon"),
            Act = () =>
            {
                _antag.ForceMakeAntag<NinjaRoleComponent>(targetPlayer, DefaultNinjaRule);
            },
            Impact = LogImpact.High,
            Message = string.Join(": ", ninjaName, Loc.GetString("admin-verb-make-space-ninja")),
        };
        args.Verbs.Add(ninja);

        if (HasComp<HumanoidAppearanceComponent>(args.Target)) // only humanoids can be cloned
            args.Verbs.Add(paradox);

        // DS14-start
        var eventRoleName = Loc.GetString("admin-verb-text-make-event-role");
        Verb eventRole = new()
        {
            Priority = -1,
            Text = eventRoleName,
            Category = VerbCategory.Antag,
            Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/_DeadSpace/Interface/Misc/antag_icons.rsi"), "Event"),
            Act = () =>
            {
                if (HasComp<EventRoleComponent>(args.Target))
                    RemComp<EventRoleComponent>(args.Target);
                else
                    EnsureComp<EventRoleComponent>(args.Target);
            },
            Impact = LogImpact.High,
            Message = string.Join(": ", eventRoleName, Loc.GetString("admin-verb-make-event-role")),
        };
        args.Verbs.Add(eventRole);

        var traitorUltraAnnouncedName = Loc.GetString("admin-verb-text-make-traitor-ultra-announced");
        Verb traitorUltraAnnounced = new()
        {
            Priority = -2,
            Text = traitorUltraAnnouncedName,
            Category = VerbCategory.Antag,
            Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/Interface/Misc/job_icons.rsi"), "SyndicateUltra"),
            Act = () =>
            {
                _traitorUltra.MakeAdminTraitorUltra(targetPlayer, announceBounty: true);
            },
            Impact = LogImpact.High,
            Message = string.Join(": ", traitorUltraAnnouncedName, Loc.GetString("admin-verb-make-traitor-ultra-announced")),
        };
        args.Verbs.Add(traitorUltraAnnounced);

        var traitorUltraSilentName = Loc.GetString("admin-verb-text-make-traitor-ultra-silent");
        Verb traitorUltraSilent = new()
        {
            Priority = -3,
            Text = traitorUltraSilentName,
            Category = VerbCategory.Antag,
            Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/Interface/Misc/job_icons.rsi"), "SyndicateUltra"),
            Act = () =>
            {
                _traitorUltra.MakeAdminTraitorUltra(targetPlayer, announceBounty: false);
            },
            Impact = LogImpact.High,
            Message = string.Join(": ", traitorUltraSilentName, Loc.GetString("admin-verb-make-traitor-ultra-silent")),
        };
        args.Verbs.Add(traitorUltraSilent);
        // DS14-end
    }

    // DS14-start
    private bool TryGetTraitorUltraRule(out Entity<TraitorUltraRuleComponent, AntagSelectionComponent> rule)
    {
        var query = EntityQueryEnumerator<TraitorUltraRuleComponent, AntagSelectionComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var traitorUltra, out var antagSelection, out var gameRule))
        {
            if (!_gameTicker.IsGameRuleAdded(uid, gameRule))
                continue;

            rule = (uid, traitorUltra, antagSelection);
            return true;
        }

        rule = default;
        return false;
    }
    // DS14-end
}
