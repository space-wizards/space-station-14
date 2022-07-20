using Content.Server.GameTicking.Rules;
using Content.Server.Mind.Components;
using Content.Server.Zombies;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;

namespace Content.Server.Administration.Systems;

public sealed partial class AdminVerbSystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly ZombifyOnDeathSystem _zombify = default!;

    // All antag verbs have names so invokeverb works.
    private void AddAntagVerbs(GetVerbsEvent<Verb> args)
    {
        if (!EntityManager.TryGetComponent<ActorComponent?>(args.User, out var actor))
            return;

        var player = actor.PlayerSession;

        if (!_adminManager.HasAdminFlag(player, AdminFlags.Fun))
            return;

        var targetHasMind = TryComp(args.Target, out MindComponent? targetMindComp);
        if (!targetHasMind || targetMindComp == null)
            return;

        Verb traitor = new()
        {
            Text = Loc.GetString("admin-antag-traitor"),
            Category = VerbCategory.Antag,
            IconTexture = "/Textures/Structures/Wallmounts/posters.rsi/poster5_contraband.png",
            Act = () =>
            {
                if (targetMindComp == null || targetMindComp.Mind == null || targetMindComp.Mind.Session == null)
                    return;

                EntitySystem.Get<TraitorRuleSystem>().MakeTraitor(targetMindComp.Mind.Session);
            },
            Impact = LogImpact.High,
            Message = "Recruit the target into the Syndicate immediately.",
        };
        args.Verbs.Add(traitor);

        Verb zombie = new()
        {
            Text = Loc.GetString("admin-antag-zombie"),
            Category = VerbCategory.Antag,
            IconTexture = "/Textures/Structures/Wallmounts/signs.rsi/bio.png",
            Act = () =>
            {
                TryComp(args.Target, out MindComponent? mindComp);
                if (mindComp == null || mindComp.Mind == null)
                    return;

                _zombify.ZombifyEntity(targetMindComp.Owner);
            },
            Impact = LogImpact.High,
            Message = "Zombifies the target.",
        };
        args.Verbs.Add(zombie);


        Verb nukeOp = new()
        {
            Text = Loc.GetString("admin-antag-nukeop"),
            Category = VerbCategory.Antag,
            IconTexture = "/Textures/Structures/Wallmounts/signs.rsi/radiation.png",
            Act = () =>
            {
                if (targetMindComp == null || targetMindComp.Mind == null || targetMindComp.Mind.Session == null)
                    return;

                EntitySystem.Get<NukeopsRuleSystem>().MakeLoneNukie(targetMindComp.Mind);
            },
            Impact = LogImpact.High,
            Message = "Make target a lone Nuclear Operative.",
        };
        args.Verbs.Add(nukeOp);

        Verb pirate = new()
        {
            Text = Loc.GetString("admin-antag-pirate"),
            Category = VerbCategory.Antag,
            IconTexture = "/Textures/Clothing/Head/Hats/pirate.rsi/icon.png",
            Act = () =>
            {
                if (targetMindComp == null || targetMindComp.Mind == null || targetMindComp.Mind.Session == null)
                    return;

                EntitySystem.Get<PiratesRuleSystem>().MakePirate(targetMindComp.Mind);
            },
            Impact = LogImpact.High,
            Message = "Shiver the target's timbers",
        };
        args.Verbs.Add(pirate);

    }
}
