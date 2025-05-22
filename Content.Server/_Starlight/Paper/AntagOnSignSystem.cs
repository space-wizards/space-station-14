using Content.Server.Antag;
using Content.Server.Antag.Components;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared._Starlight.Paper;
using Content.Shared.Paper;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Starlight.Paper;

public sealed class AntagOnSignSystem : EntitySystem
{
    
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    
    private readonly EntProtoId _paradoxCloneRuleId = "ParadoxCloneSpawn";
    
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AntagOnSignComponent,PaperSignedEvent>(OnPaperSigned);
    }

    private void OnPaperSigned(EntityUid uid, AntagOnSignComponent component, PaperSignedEvent args)
    {
        if (component.Used)
            return;

        if (!TryComp(args.Signer, out ActorComponent? actor))
                return;

        if (_random.NextFloat() > component.Chance)
        {
            component.Used = true;
            return;
        }
        
        var session = actor.PlayerSession;
        foreach (var antag in component.Antags)
        {
            _antag.ForceMakeAntag<GameRuleComponent>(session, antag.Id);
        }

        if (component.ParadoxClone)
        {
            var ruleEnt = _gameTicker.AddGameRule(_paradoxCloneRuleId);

            if (!TryComp<ParadoxCloneRuleComponent>(ruleEnt, out var paradoxCloneRuleComp))
                component.Used = true;
                return;

            paradoxCloneRuleComp.OriginalBody = args.Signer; // override the target player

            _gameTicker.StartGameRule(ruleEnt);
        }
        
        component.Used = true;
    }
}