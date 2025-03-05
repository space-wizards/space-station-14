using Robust.Shared.Random;

namespace Content.Server.Speech.EntitySystems;

public sealed class SingleAccentuator
{

    private readonly EntitySystem? _accentSystem;

    public SingleAccentuator()
    {
        _accentSystem = GetRandomAccentSystem();
    }

    private EntitySystem GetRandomAccentSystem()
    {
        var random = IoCManager.Resolve<IRobustRandom>();
        var entMan = IoCManager.Resolve<IEntityManager>();

        var accentSystems = new List<EntitySystem>
        {
            entMan.EntitySysManager.GetEntitySystem<OwOAccentSystem>(),
            entMan.EntitySysManager.GetEntitySystem<GermanAccentSystem>(),
            entMan.EntitySysManager.GetEntitySystem<RussianAccentSystem>(),
        };
        return random.Pick(accentSystems);
    }

    public string Accentuate(string message)
    {
        switch (_accentSystem)
        {
            case OwOAccentSystem owoAccentSystem:
                return owoAccentSystem.Accentuate(message);
            case GermanAccentSystem germanAccentSystem:
                return germanAccentSystem.Accentuate(message);
            case RussianAccentSystem russianAccentSystem:
                return russianAccentSystem.Accentuate(message);
        }

        return message;
    }
}
