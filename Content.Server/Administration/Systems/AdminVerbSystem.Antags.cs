using Content.Server.GameTicking;
using Content.Server.Zombies;
using Content.Shared.Administration;
using Content.Server.Clothing.Systems;
using Content.Shared.Antag;
using Content.Shared.Database;
using Content.Shared.GameTicking.Rules.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mind.Components;
using Content.Shared.Roles;
using Content.Shared.Verbs;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Content.Shared.Roles.Components;

namespace Content.Server.Administration.Systems;

public sealed partial class AdminVerbSystem
{
    [Dependency] private ServerGameTicker _gameTicker = default!;
    [Dependency] private AntagSelectionSystem _antag = default!;
    [Dependency] private OutfitSystem _outfit = default!;
    [Dependency] private ZombieSystem _zombie = default!;

    [Dependency] private EntityQuery<ActorComponent> _actorQuery;
    [Dependency] private EntityQuery<HumanoidProfileComponent> _humanoidQuery;
    [Dependency] private EntityQuery<MindContainerComponent> _mindContainerQuery;

    private static readonly EntProtoId TraitorRule = "Traitor";
    private static readonly ProtoId<AntagSpecifierPrototype> TraitorSpec = "Traitor";
    private static readonly EntProtoId InitialInfectedRule = "Zombie";
    private static readonly ProtoId<AntagSpecifierPrototype> InitialInfectedSpec = "InitialInfected";
    private static readonly EntProtoId LoneOpRule = "LoneOpsSpawn";
    private static readonly ProtoId<AntagSpecifierPrototype> LoneOpSpec = "LoneOp";
    private static readonly EntProtoId RevsRule = "Revolutionary";
    private static readonly ProtoId<AntagSpecifierPrototype> RevsSpec = "HeadRev";
    private static readonly EntProtoId ThiefRule = "Thief";
    private static readonly ProtoId<AntagSpecifierPrototype> ThiefSpec = "Thief";
    private static readonly EntProtoId ChangelingRule = "Changeling";
    private static readonly ProtoId<AntagSpecifierPrototype> ChangelingSpec = "Changeling";
    private static readonly EntProtoId ParadoxCloneRule = "ParadoxCloneSpawn";
    private static readonly EntProtoId WizardRule = "Wizard";
    private static readonly ProtoId<AntagSpecifierPrototype> WizardSpec = "Wizard";
    private static readonly EntProtoId NinjaRule = "NinjaSpawn";
    private static readonly ProtoId<AntagSpecifierPrototype> NinjaSpec = "SpaceNinja";
    private static readonly ProtoId<StartingGearPrototype> PirateGearId = "PirateGear";

    // All antag verbs have names so invokeverb works.
    private void AddAntagVerbs(GetVerbsEvent<Verb> args)
    {
        if (!_actorQuery.TryComp(args.User, out var actor))
            return;

        var player = actor.PlayerSession;

        if (!_adminManager.HasAdminFlag(player, AdminFlags.Fun))
            return;

        if (!_mindContainerQuery.HasComp(args.Target) || !_actorQuery.TryComp(args.Target, out var targetActor))
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
                _antag.ForceMakeAntag<TraitorRuleComponent>(targetPlayer, TraitorRule, TraitorSpec);
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
                _antag.ForceMakeAntag<ZombieRuleComponent>(targetPlayer, InitialInfectedRule, InitialInfectedSpec);
            },
            Impact = LogImpact.High,
            Message = string.Join(": ", initialInfectedName, Loc.GetString("admin-verb-make-initial-infected")),
        };
        args.Verbs.Add(initialInfected);

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
                _antag.ForceMakeAntag<NukeopsRuleComponent>(targetPlayer, LoneOpRule, LoneOpSpec);
            },
            Impact = LogImpact.High,
            Message = string.Join(": ", nukeOpName, Loc.GetString("admin-verb-make-nuclear-operative")),
        };
        args.Verbs.Add(nukeOp);

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
                _antag.ForceMakeAntag<RevolutionaryRuleComponent>(targetPlayer, RevsRule, RevsSpec);
            },
            Impact = LogImpact.High,
            Message = string.Join(": ", headRevName, Loc.GetString("admin-verb-make-head-rev")),
        };
        args.Verbs.Add(headRev);

        var thiefName = Loc.GetString("admin-verb-text-make-thief");
        Verb thief = new()
        {
            Text = thiefName,
            Category = VerbCategory.Antag,
            Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/Clothing/Hands/Gloves/Color/black.rsi"), "icon"),
            Act = () =>
            {
                _antag.ForceMakeAntag<ThiefRuleComponent>(targetPlayer, ThiefRule, ThiefSpec);
            },
            Impact = LogImpact.High,
            Message = string.Join(": ", thiefName, Loc.GetString("admin-verb-make-thief")),
        };
        args.Verbs.Add(thief);

        var changelingName = Loc.GetString("admin-verb-text-make-changeling");
        Verb changeling = new()
        {
            Text = changelingName,
            Category = VerbCategory.Antag,
            Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/Objects/Weapons/Melee/armblade.rsi"), "icon"),
            Act = () =>
            {
                _antag.ForceMakeAntag<ChangelingRuleComponent>(targetPlayer, ChangelingRule, ChangelingSpec);
            },
            Impact = LogImpact.High,
            Message = string.Join(": ", changelingName, Loc.GetString("admin-verb-make-changeling")),
        };
        args.Verbs.Add(changeling);

        // only humanoids can be cloned
        if (_humanoidQuery.HasComp(args.Target))
        {
            var paradoxCloneName = Loc.GetString("admin-verb-text-make-paradox-clone");
            Verb paradox = new()
            {
                Text = paradoxCloneName,
                Category = VerbCategory.Antag,
                Icon = new SpriteSpecifier.Rsi(new("/Textures/Interface/Misc/job_icons.rsi"), "ParadoxClone"),
                Act = () =>
                {
                    if (_gameTicker.AddGameRule(ParadoxCloneRule) is not { } ruleEnt)
                        return;

                    if (!TryComp<ParadoxCloneRuleComponent>(ruleEnt, out var paradoxCloneRuleComp))
                        return;

                    paradoxCloneRuleComp.OriginalBody = args.Target; // override the target player

                    _gameTicker.StartGameRule(ruleEnt.AsNullable());
                },
                Impact = LogImpact.High,
                Message = string.Join(": ", paradoxCloneName, Loc.GetString("admin-verb-make-paradox-clone")),
            };

            args.Verbs.Add(paradox);
        }

        var wizardName = Loc.GetString("admin-verb-text-make-wizard");
        Verb wizard = new()
        {
            Text = wizardName,
            Category = VerbCategory.Antag,
            Icon = new SpriteSpecifier.Rsi(new("/Textures/Interface/Misc/job_icons.rsi"), "Wizard"),
            Act = () =>
            {
                // Wizard has no rule components as of writing, but I gotta put something here to satisfy the machine so just make it wizard mind rule :)
                _antag.ForceMakeAntag<WizardRoleComponent>(targetPlayer, WizardRule, WizardSpec);
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
                _antag.ForceMakeAntag<NinjaRoleComponent>(targetPlayer, NinjaRule, NinjaSpec);
            },
            Impact = LogImpact.High,
            Message = string.Join(": ", ninjaName, Loc.GetString("admin-verb-make-space-ninja")),
        };
        args.Verbs.Add(ninja);
    }
}
