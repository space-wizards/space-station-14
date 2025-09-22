using Content.Shared.Database;
using Content.Shared.Humanoid;
using Content.Shared.Mind.Components;
using Content.Shared.Verbs;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Shared.Administration;

public abstract partial class SharedAdminVerbSystem
{
    // All antag verbs have names so invokeverb works.
    private void AddAntagVerbs(GetVerbsEvent<Verb> args)
    {
        if (!TryComp<ActorComponent>(args.User, out var actor))
            return;

        var player = actor.PlayerSession;

        if (!_sharedAdmin.HasAdminFlag(player, AdminFlags.Fun))
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
            Act = () => AntagForceTraitorVerb(targetPlayer),
            Impact = LogImpact.High,
            Message = string.Join(": ", traitorName, Loc.GetString("admin-verb-make-traitor")),
        };
        args.Verbs.Add(traitor);

        var initialInfectedName = Loc.GetString("admin-verb-text-make-initial-infected");
        Verb initialInfected = new()
        {
            Text = initialInfectedName,
            Category = VerbCategory.Antag,
            Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/Interface/Misc/job_icons.rsi"), "InitialInfected"),
            Act = () => AntagForceInitialInfectedVerb(targetPlayer),
            Impact = LogImpact.High,
            Message = string.Join(": ", initialInfectedName, Loc.GetString("admin-verb-make-initial-infected")),
        };
        args.Verbs.Add(initialInfected);

        var zombieName = Loc.GetString("admin-verb-text-make-zombie");
        Verb zombie = new()
        {
            Text = zombieName,
            Category = VerbCategory.Antag,
            Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/Interface/Misc/job_icons.rsi"), "Zombie"),
            Act = () => AntagForceZombifyVerb(args.Target),
            Impact = LogImpact.High,
            Message = string.Join(": ", zombieName, Loc.GetString("admin-verb-make-zombie")),
        };
        args.Verbs.Add(zombie);

        var nukeOpName = Loc.GetString("admin-verb-text-make-nuclear-operative");
        Verb nukeOp = new()
        {
            Text = nukeOpName,
            Category = VerbCategory.Antag,
            Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/Clothing/Head/Hardsuits/syndicate.rsi"), "icon"),
            Act = () => AntagForceNukeOpsVerb(targetPlayer),
            Impact = LogImpact.High,
            Message = string.Join(": ", nukeOpName, Loc.GetString("admin-verb-make-nuclear-operative")),
        };
        args.Verbs.Add(nukeOp);

        var pirateName = Loc.GetString("admin-verb-text-make-pirate");
        Verb pirate = new()
        {
            Text = pirateName,
            Category = VerbCategory.Antag,
            Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/Clothing/Head/Hats/pirate.rsi"), "icon"),
            Act = () => AntagForcePirateVerb(args.Target),
            Impact = LogImpact.High,
            Message = string.Join(": ", pirateName, Loc.GetString("admin-verb-make-pirate")),
        };
        args.Verbs.Add(pirate);

        var headRevName = Loc.GetString("admin-verb-text-make-head-rev");
        Verb headRev = new()
        {
            Text = headRevName,
            Category = VerbCategory.Antag,
            Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/Interface/Misc/job_icons.rsi"), "HeadRevolutionary"),
            Act = () => AntagForceRevVerb(targetPlayer),
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
            Act = () => AntagForceThiefVerb(targetPlayer),
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
            Act = () => AntagForceChanglingVerb(targetPlayer),
            Impact = LogImpact.High,
            Message = string.Join(": ", changelingName, Loc.GetString("admin-verb-make-changeling")),
        };
        args.Verbs.Add(changeling);

        if (HasComp<HumanoidAppearanceComponent>(args.Target)) // only humanoids can be cloned
        {
            var paradoxCloneName = Loc.GetString("admin-verb-text-make-paradox-clone");
            Verb paradox = new()
            {
                Text = paradoxCloneName,
                Category = VerbCategory.Antag,
                Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/Interface/Misc/job_icons.rsi"), "ParadoxClone"),
                Act = () => AntagForceParadoxCloneVerb(args.Target),
                Impact = LogImpact.High,
                Message = string.Join(": ", paradoxCloneName, Loc.GetString("admin-verb-make-paradox-clone")),
            };

            args.Verbs.Add(paradox);
        }
    }

    protected virtual void AntagForceTraitorVerb(ICommonSession target)
    {
    }

    protected virtual void AntagForceInitialInfectedVerb(ICommonSession target)
    {
    }

    protected virtual void AntagForceZombifyVerb(EntityUid target)
    {
    }

    protected virtual void AntagForceNukeOpsVerb(ICommonSession target)
    {
    }

    protected virtual void AntagForcePirateVerb(EntityUid target)
    {
    }

    protected virtual void AntagForceRevVerb(ICommonSession target)
    {
    }

    protected virtual void AntagForceThiefVerb(ICommonSession target)
    {
    }

    protected virtual void AntagForceChanglingVerb(ICommonSession target)
    {
    }

    protected virtual void AntagForceParadoxCloneVerb(EntityUid target)
    {
    }
}
