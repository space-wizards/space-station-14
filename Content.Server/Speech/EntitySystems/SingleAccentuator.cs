using Robust.Shared.Random;

namespace Content.Server.Speech.EntitySystems;

public sealed class SingleAccentuator
{
    private EntitySystem? _accentSystem;

    private readonly IReadOnlyList<EntitySystem> _accentSystems;

    public SingleAccentuator()
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        _accentSystems = new List<EntitySystem>
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
        NextSystem();
    }

    public void NextSystem()
    {
        _accentSystem = GetRandomAccentSystem();
    }

    private EntitySystem GetRandomAccentSystem()
    {
        var random = IoCManager.Resolve<IRobustRandom>();

        return random.Pick(_accentSystems);
    }

    public string Accentuate(string message)
    {
        return _accentSystem switch
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
            _ => message
        };
    }
}
