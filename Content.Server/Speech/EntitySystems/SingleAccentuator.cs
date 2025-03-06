using System.Linq;
using Robust.Shared.Random;

namespace Content.Server.Speech.EntitySystems;

public sealed class SingleAccentuator
{
    private List<EntitySystem> _activeAccentSystems;

    private readonly IReadOnlyList<EntitySystem> _accentSystemCandidates;

    private const int MaxAccentIterations = 3;

    private const float ReaccentuationChance = 0.5f;

    public SingleAccentuator()
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        _accentSystemCandidates = new List<EntitySystem>
        {
            entMan.EntitySysManager.GetEntitySystem<OwOAccentSystem>(),
            entMan.EntitySysManager.GetEntitySystem<GermanAccentSystem>(),
            entMan.EntitySysManager.GetEntitySystem<RussianAccentSystem>(),
            entMan.EntitySysManager.GetEntitySystem<FrenchAccentSystem>(),
            entMan.EntitySysManager.GetEntitySystem<MumbleAccentSystem>(),
            entMan.EntitySysManager.GetEntitySystem<SlurredSystem>(),
            entMan.EntitySysManager.GetEntitySystem<MobsterAccentSystem>(),
            entMan.EntitySysManager.GetEntitySystem<PirateAccentSystem>(),
            entMan.EntitySysManager.GetEntitySystem<MonkeyAccentSystem>(),
            entMan.EntitySysManager.GetEntitySystem<StutteringSystem>(),
        };

        _activeAccentSystems = new();
        NextActiveAccentSystemArray();
    }

    public void NextActiveAccentSystemArray()
    {
        _activeAccentSystems.Clear();

        var random = IoCManager.Resolve<IRobustRandom>();

        var accentSystemCandidates = _accentSystemCandidates.ToList();

        for (var i = 0; i < MaxAccentIterations; i++)
        {
            if (accentSystemCandidates.Count == 0)
                break;

            if (!random.Prob(ReaccentuationChance))
                continue;

            var accentSystem = random.PickAndTake(accentSystemCandidates);
            _activeAccentSystems.Add(accentSystem);
        }
    }

    public string Accentuate(string message)
    {
        foreach (var accentSystem in _activeAccentSystems)
        {
            message = AccentuateSingle(message, accentSystem);
        }

        return message;
    }

    private static string AccentuateSingle(string message, EntitySystem accentSystem)
    {
        return accentSystem switch
        {
            OwOAccentSystem owoAccentSystem => owoAccentSystem.Accentuate(message),
            GermanAccentSystem germanAccentSystem => germanAccentSystem.Accentuate(message),
            RussianAccentSystem russianAccentSystem => russianAccentSystem.Accentuate(message),
            FrenchAccentSystem frenchAccentSystem => frenchAccentSystem.Accentuate(message),
            MumbleAccentSystem mumbleAccentSystem => mumbleAccentSystem.Accentuate(message),
            SlurredSystem slurredSystem => slurredSystem.Accentuate(message),
            MobsterAccentSystem mobsterAccentSystem => mobsterAccentSystem.Accentuate(message),
            PirateAccentSystem pirateAccentSystem => pirateAccentSystem.Accentuate(message),
            MonkeyAccentSystem monkeyAccentSystem => monkeyAccentSystem.Accentuate(message),
            StutteringSystem stutteringSystem => stutteringSystem.Accentuate(message),
            _ => message,
        };
    }
}
