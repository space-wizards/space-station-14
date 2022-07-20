using System.Linq;
using System.Threading;
using Content.Server.Administration.Commands;
using Content.Server.Administration.Components;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Clothing.Components;
using Content.Server.Damage.Systems;
using Content.Server.Disease;
using Content.Server.Disease.Components;
using Content.Server.Electrocution;
using Content.Server.Explosion.EntitySystems;
using Content.Server.GameTicking.Rules;
using Content.Server.GhostKick;
using Content.Server.Interaction.Components;
using Content.Server.Medical;
using Content.Server.Mind.Components;
using Content.Server.Nutrition.EntitySystems;
using Content.Server.Pointing.Components;
using Content.Server.Polymorph.Systems;
using Content.Server.Popups;
using Content.Server.Tabletop;
using Content.Server.Tabletop.Components;
using Content.Server.Traitor;
using Content.Server.Traitor.Uplink;
using Content.Server.Traitor.Uplink.Account;
using Content.Server.Zombies;
using Content.Shared.Administration;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.CCVar;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.Disease;
using Content.Shared.Electrocution;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory;
using Content.Shared.MobState;
using Content.Shared.MobState.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Content.Shared.Tabletop.Components;
using Content.Shared.Traitor.Uplink;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.Administration.Systems;

public sealed partial class AdminVerbSystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly ZombifyOnDeathSystem _zombify = default!;
    private const string TraitorPrototypeID = "Traitor";
    // All antag verbs have names so invokeverb works.
    private void AddAntagVerbs(GetVerbsEvent<Verb> args)
    {
        if (!EntityManager.TryGetComponent<ActorComponent?>(args.User, out var actor))
            return;

        var player = actor.PlayerSession;

        if (!_adminManager.HasAdminFlag(player, AdminFlags.Fun))
            return;

        var targetHasMind = TryComp(args.Target, out MindComponent? targetMindComp);
        if (targetMindComp == null)
            return;

        Verb traitor = new()
        {
            Text = Loc.GetString("admin-antag-traitor"),
            Category = VerbCategory.Antag,
            IconTexture = "/Textures/Interface/VerbIcons/antag-e_sword-temp.192dpi.png",
            Act = () =>
            {
                if (targetMindComp == null || targetMindComp.Mind == null || targetMindComp.Mind.Session == null)
                    return;

                EntitySystem.Get<TraitorRuleSystem>().MakeTraitor(targetMindComp.Mind.Session);
            },
            Impact = LogImpact.High,
            Message = "Recruit the target into the Syndicate immediately.",
        };
        if(targetHasMind)
            args.Verbs.Add(traitor);

        Verb zombie = new()
        {
            Text = Loc.GetString("admin-antag-zombie"),
            Category = VerbCategory.Antag,
            IconTexture = "/Textures/Interface/VerbIcons/zombify-temp.png",
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
        if (targetHasMind)
            args.Verbs.Add(zombie);

        //Remaining entries:



    }
}
