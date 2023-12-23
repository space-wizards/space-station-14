using Content.Shared.Traits.Assorted;
using Content.Server.Speech;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Prototypes;
using Content.Server.GameTicking;
using Content.Shared.Dataset;

namespace Content.Server.Traits.Assorted;

public sealed class PsychosisSystem : SharedPsychosisSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private static string _firstHeal = "";

    private static string _secondHeal = "";

    private static string _thirdHeal = "";
    private static readonly IReadOnlyDictionary<string, string> SpecialWords = new Dictionary<string, string>()
    {
        { " и ", " " },
        { " в ", " " },
        { " а ", " " },
        { " но ", " " },
        { " я ", " " },
        { " ну ", " " },
        { "а", "" },
        { "я", "" },
        { "о", "" },
    };

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PsychosisComponent, AccentGetEvent>(OnAccent);
        SubscribeNetworkEvent<StageChange>(StageChanged);
        SubscribeLocalEvent<PsychosisComponent, ComponentStartup>(OnStart);
        SubscribeLocalEvent<RoundStartAttemptEvent>(Generate);
    }
    public string Accentuate(string message)
    {
        foreach (var (word, repl) in SpecialWords)
        {
            message = message.Replace(word, repl);
        }
        return message;
    }
    private void Generate(RoundStartAttemptEvent args)
    {
        _firstHeal = _random.Pick(_prototypeManager.Index<DatasetPrototype>("PsychosisHealsFirst").Values);
        _secondHeal = _random.Pick(_prototypeManager.Index<DatasetPrototype>("PsychosisHealsSecond").Values);
        _thirdHeal = _random.Pick(_prototypeManager.Index<DatasetPrototype>("PsychosisHealsThird").Values);
    }
    public string GetFirst()
    {
        return _firstHeal;
    }
    public string GetSecond()
    {
        return _secondHeal;
    }
    public string GetThird()
    {
        return _thirdHeal;
    }

    private void OnStart(EntityUid uid, PsychosisComponent component, ComponentStartup args)
    {
        component.HealFirst = _firstHeal;
        component.HealSecond = _secondHeal;
        component.HealThird = _thirdHeal;
    }

    private void StageChanged(StageChange psychosis, EntitySessionEventArgs args)
    {
        if (!TryComp<PsychosisComponent>(GetEntity(psychosis.Psychosis), out var psych))
            return;
        psych.Stage = psychosis.Stage;
    }
    private void OnAccent(EntityUid uid, PsychosisComponent component, AccentGetEvent args)
    {
        if (component.Stage > 1)
        {
            var chance = 0.30f * (component.Stage - 1);
            if (_random.Prob(chance))
            {
                args.Message = Accentuate(args.Message);
            }
        }
    }

}
