using Content.Server.Administration.Verbs.Operations;
using Content.Server.Administration.Verbs.Prototypes;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;

namespace Content.Server.Administration.Systems;

public sealed partial class AdminVerbSystem
{
    [Dependency] private AdminOperationSystem _adminOperations = default!;
    [Dependency] private EntityWhitelistSystem _whitelistSystem = default!;

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

        foreach (var prototype in ProtoMan.EnumeratePrototypes<AdminSmitePrototype>())
        {
            if (!_whitelistSystem.CheckBoth(args.Target, prototype.Blacklist, prototype.Whitelist))
                continue;

            var name = Loc.GetString(prototype.Name).ToLowerInvariant();
            var verb = new Verb
            {
                Text = name,
                Category = VerbCategory.Smite,
                Icon = prototype.Icon,
                Act = () => _adminOperations.Execute(args.Target, args.User, prototype.Operations),
                Impact = LogImpact.Extreme,
                Message = prototype.Description is { } description
                    ? string.Join(": ", name, Loc.GetString(description))
                    : null
            };

            args.Verbs.Add(verb);
        }
    }
}
