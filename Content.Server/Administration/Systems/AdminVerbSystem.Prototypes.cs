using Content.Shared.Administration.Verbs.Prototypes;
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
            var category = ProtoMan.Index(prototype.CategoryPrototype);
            if (!_adminManager.HasAdminFlag(player, category.RequiredFlags))
                continue;

            if (!_whitelistSystem.CheckBoth(args.Target, category.Blacklist, category.Whitelist) ||
                !_whitelistSystem.CheckBoth(args.Target, prototype.Blacklist, prototype.Whitelist))
                continue;

            var name = Loc.GetString(prototype.Name).ToLowerInvariant();
            var verb = new Verb
            {
                Text = name,
                Category = new VerbCategory(category.Name, category.Icon, category.IconsOnly)
                {
                    Columns = category.Columns
                },
                Icon = prototype.Icon,
                Act = () => _entityEffects.ApplyEffects(args.Target, prototype.Effects, user: args.User),
                Impact = category.Impact,
                Message = prototype.Description is { } description
                    ? string.Join(": ", name, Loc.GetString(description))
                    : null
            };

            args.Verbs.Add(verb);
        }
    }
}
