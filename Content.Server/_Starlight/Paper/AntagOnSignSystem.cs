using Content.Server.Antag;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.Paper;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Starlight.Paper;

public sealed class AntagOnSignSystem : EntitySystem
{

    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    private ISawmill _sawmill = default!;


    private readonly EntProtoId _paradoxCloneRuleId = "ParadoxCloneSpawn";

    public override void Initialize()
    {
        base.Initialize();
        _sawmill = Logger.GetSawmill(this.SawmillName);
        SubscribeLocalEvent<AntagOnSignComponent, PaperSignedEvent>(OnPaperSigned);
    }

    private void OnPaperSigned(EntityUid uid, AntagOnSignComponent component, PaperSignedEvent args)
    {
        if (component.ChargesRemaing <= 0)
            return;
        var signer = args.Signer;
        if (!TryComp(signer, out ActorComponent? actor))
            return;
        if (component.SignedEntityUids.Contains(signer))
            return;
        component.ChargesRemaing--;
        component.SignedEntityUids.Add(signer);

        if (_random.NextFloat() > component.Chance)
            return;


        var session = actor.PlayerSession;
        foreach (var antag in component.Antags)
        {
            var targetComp = _componentFactory.GetComponent(antag.TargetComponent);

            var fmakeantag = typeof(AntagSelectionSystem).GetMethod(nameof(AntagSelectionSystem.ForceMakeAntag));
            if (fmakeantag == null)
            {
                _sawmill.Error("Failed to reflect \"ForceMakeAntag\" method from AntagSelectionSystem for genericization");
                continue;
            }
            var generic = fmakeantag.MakeGenericMethod(targetComp.GetType());
            generic.Invoke(_antag, [session, antag.Antag.Id]);

            //_antag.ForceMakeAntag<GameRuleComponent>(session, antag.Id);
        }

        if (component.ParadoxClone)
        {
            var ruleEnt = _gameTicker.AddGameRule(_paradoxCloneRuleId);

            if (!TryComp<ParadoxCloneRuleComponent>(ruleEnt, out var paradoxCloneRuleComp))
                return;

            paradoxCloneRuleComp.OriginalBody = args.Signer; // override the target player

            _gameTicker.StartGameRule(ruleEnt);
        }
    }
}
