using Content.Server.Administration.Verbs.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Player;

namespace Content.Server.Administration.Systems;

public sealed partial class AdminVerbSystem
{
    [Dependency] private SharedEntityEffectsSystem _entityEffects = default!;
    [Dependency] private EntityWhitelistSystem _whitelistSystem = default!;

    // All prototype verbs have names so invokeverb works.
    private void AddPrototypeVerbs(GetVerbsEvent<Verb> args)
    {
        if (!TryComp(args.User, out ActorComponent? actor))
            return;

        var player = actor.PlayerSession;

        foreach (var prototype in ProtoMan.EnumeratePrototypes<AdminVerbPrototype>())
        {
            if (prototype.Abstract)
                continue;

            if (!_adminManager.HasAdminFlag(player, prototype.RequiredFlags))
                continue;

            if (!_whitelistSystem.CheckBoth(args.Target, prototype.Blacklist, prototype.Whitelist))
                continue;

            var name = Loc.GetString(prototype.Name).ToLowerInvariant();
            var verb = new Verb
            {
                Text = name,
                Category = prototype.Category is { } category
                    ? new VerbCategory(category, prototype.CategoryIcon, prototype.CategoryIconsOnly)
                    {
                        Columns = prototype.CategoryColumns
                    }
                    : null,
                Icon = prototype.Icon,
                Act = () => _entityEffects.ApplyEffects(args.Target, prototype.Effects, user: args.User),
                Impact = prototype.Impact,
                Message = prototype.Description is { } description
                    ? string.Join(": ", name, Loc.GetString(description))
                    : null
            };

            args.Verbs.Add(verb);
        }
    }
}
