using Content.Server.GameTicking;
using Content.Shared.Paper;
using Content.Shared.Whitelist;
using Content.Shared.Fax.Components;
using Robust.Shared.Random;
using Content.Shared.Mind;
using Robust.Shared.Player;

namespace Content.Server._Starlight.Paper;

public sealed class ObjectiveOnSignSystem : EntitySystem
{
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ObjectiveOnSignComponent, PaperSignedEvent>(OnPaperSigned);
        SubscribeLocalEvent<ObjectiveOnSignComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, ObjectiveOnSignComponent comp, MapInitEvent init)
    {
        if (comp.KeepFaxable)
            return;
        RemComp<FaxableObjectComponent>(uid); //cause this breaks shit like infinite antags
    }


    private void OnPaperSigned(EntityUid uid, ObjectiveOnSignComponent component, PaperSignedEvent args)
    {
        if (component.ChargesRemaining <= 0)
            return; // no charges left. dont run the component

        var signer = args.Signer;
        
        if (!_whitelistSystem.CheckBoth(signer, component.Blacklist, component.Whitelist))
            return;

        if (!TryComp<ActorComponent>(signer, out var actor))
            return;
        if (!_mind.TryGetMind(actor.PlayerSession.UserId, out var mindId, out var mind))
        {
            Log.Error($"Antag {ToPrettyString(signer):player} signed a paper with ObjectiveOnSign but had no mind attached!");
            return;
        }

        component.ChargesRemaining--;
        component.SignedEntityUids.Add(signer);

        if (_random.NextFloat() > component.Chance)
            return; //vibe check failed no events for you.

        if (!component.Append)
        {
            while (_mind.TryRemoveObjective(mindId.Value, mind, 0)) {}// basically just keep trying to remove the 0th objective until there is none           
        }

        foreach (var rule in component.Objectives)
        {
            if (!_mind.TryAddObjective(mindId.Value, mind, rule.Id))
                Log.Warning($"Failed to add objective {rule.Id} to {ToPrettyString(signer):player}.");
        }

    }
}
