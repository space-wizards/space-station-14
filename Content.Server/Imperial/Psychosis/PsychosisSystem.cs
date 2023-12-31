using Content.Shared.Traits.Assorted;
using Content.Server.Speech;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Prototypes;
using Content.Server.GameTicking;
using Content.Shared.Dataset;
using Robust.Shared.Configuration;
using Content.Shared.Imperial.ICCVar;
using FastAccessors;

namespace Content.Server.Traits.Assorted;

public sealed class PsychosisSystem : SharedPsychosisSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    private static List<string> _firstStageHeals = new List<string>();
    private static List<string> _secondStageHeals = new List<string>();
    private static List<string> _thirdStageHeals = new List<string>();
    private static Dictionary<string, string> _popUpHealsfirst = new Dictionary<string, string>();

    private static Dictionary<string, string> _popUpHealssecond = new Dictionary<string, string>();

    private static Dictionary<string, string> _popUpHealsthird = new Dictionary<string, string>();
    private static readonly IReadOnlyDictionary<string, string> SpecialWords = new Dictionary<string, string>()
    {
        { "а", "" },
        { "я", "" },
        { "о", "" },
    };

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PsychosisComponent, AccentGetEvent>(OnAccent);
        SubscribeNetworkEvent<StageChange>(StageChanged);
        SubscribeNetworkEvent<GetPopup>(Get);
        SubscribeLocalEvent<PsychosisComponent, ComponentStartup>(OnStart);
        SubscribeLocalEvent<RoundStartAttemptEvent>(Generate);
    }
    public string Accentuate(string message, PsychosisComponent component)
    {
        foreach (var (word, repl) in SpecialWords)
        {
            message = message.Replace(word, repl);
        }
        return message;
    }
    private void Generate(RoundStartAttemptEvent args)
    {
        _popUpHealsfirst.Clear();
        _popUpHealssecond.Clear();
        _popUpHealsthird.Clear();
        _firstStageHeals.Clear();
        _secondStageHeals.Clear();
        _thirdStageHeals.Clear();
        foreach (var thing in _prototypeManager.Index<DatasetPrototype>("PsychosisHealsFirst").Values)
        {
            _firstStageHeals.Add(thing);
        }
        foreach (var thing in _prototypeManager.Index<DatasetPrototype>("PsychosisHealsSecond").Values)
        {
            _secondStageHeals.Add(thing);
        }
        foreach (var thing in _prototypeManager.Index<DatasetPrototype>("PsychosisHealsThird").Values)
        {
            _thirdStageHeals.Add(thing);
        }
        foreach (var popup in _prototypeManager.Index<DatasetPrototype>("PsychosisPopupsTable").Values)
        {
            _popUpHealsfirst.Add(popup, _random.PickAndTake(_firstStageHeals));
            _popUpHealssecond.Add(popup, _random.PickAndTake(_secondStageHeals));
            _popUpHealsthird.Add(popup, _random.PickAndTake(_thirdStageHeals));
        }
    }
    public string GetHeal(string popup, int stage)
    {
        if (stage == 1)
        {
            if (!_popUpHealsfirst.TryGetValue(popup, out var first))
                return "";
            return first;
        }
        if (stage == 2)
        {
            if (!_popUpHealssecond.TryGetValue(popup, out var second))
                return "";
            return second;
        }
        if (stage == 3)
        {
            if (!_popUpHealsthird.TryGetValue(popup, out var third))
                return "";
            return third;
        }
        return "";
    }

    private void OnStart(EntityUid uid, PsychosisComponent component, ComponentStartup args)
    {
        var dataset = _prototypeManager.Index<DatasetPrototype>("PsychosisPopupsTable").Values;
        component.PopUp = _random.Pick(dataset);
        if (!_popUpHealsfirst.TryGetValue(component.PopUp, out var first))
            return;
        if (!_popUpHealssecond.TryGetValue(component.PopUp, out var second))
            return;
        if (!_popUpHealsthird.TryGetValue(component.PopUp, out var third))
            return;
        component.HealFirst = first;
        component.HealSecond = second;
        component.HealThird = third;
    }

    private void Get(GetPopup psychosis, EntitySessionEventArgs args)
    {
        if (!TryComp<PsychosisComponent>(GetEntity(psychosis.Psychosis), out var psych))
            return;
        var msg = new PopUpTransfer(psych.PopUp, GetNetEntity(psych.Owner));
        RaiseNetworkEvent(msg);
    }

    private void StageChanged(StageChange psychosis, EntitySessionEventArgs args)
    {
        if (!TryComp<PsychosisComponent>(GetEntity(psychosis.Psychosis), out var psych))
            return;
        psych.Stage = psychosis.Stage;
    }
    private void OnAccent(EntityUid uid, PsychosisComponent component, AccentGetEvent args)
    {
        if (_cfg.GetCVar(ICCVars.PsychosisEnabled) == true)
        {
            if (component.Stage > 1)
            {
                if (component.Stage > 2)
                {
                    var messages = args.Message.Split(" ");
                    foreach (var messagething in messages)
                    {
                        if (_random.Prob(0.3f))
                        {
                            args.Message = args.Message.Replace(messagething, "");
                        }
                    }
                }
                var chance = 0.20f * (component.Stage - 1);
                if (_random.Prob(chance))
                {
                    args.Message = Accentuate(args.Message, component);
                }
            }
        }
    }

}
