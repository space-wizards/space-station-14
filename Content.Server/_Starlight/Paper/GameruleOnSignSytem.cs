using Content.Server.GameTicking;
using Content.Shared.Paper;
using Content.Shared.Whitelist;
using Robust.Shared.Random;

namespace Content.Server._Starlight.Paper;

public sealed class GameruleOnSignSytem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GameruleOnSignComponent,PaperSignedEvent>(OnPaperSigned);
    }

    private void OnPaperSigned(EntityUid uid, GameruleOnSignComponent component, PaperSignedEvent args)
    {
        if (component.Remaining <= 0)
            return; // we allready ran this component so no need to check again anymore.
        var signer = args.Signer;
        if (!_whitelistSystem.CheckBoth(signer, component.Blacklist, component.Whitelist))
            return;
        component.Remaining--;
        component.SignedEntityUids.Add(signer);

        if (component.Remaining != 0)
            return; //we havent hit the conditions to activate it.
        
        if (_random.NextFloat() > component.Chance)
            return; //vibe check failed no events for you.

        foreach (var rule in component.Rules)
        {
            var ent = _gameTicker.AddGameRule(rule.Id);
            _gameTicker.StartGameRule(ent);
        }
        
    }
}