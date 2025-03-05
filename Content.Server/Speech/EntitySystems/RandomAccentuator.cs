using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Speech.EntitySystems;

public sealed class RandomAccentuator
{
    private const float DefaultAccentuationChance = 1.0f;

    public string MaybeAccentuate(string message, float chance = DefaultAccentuationChance)
    {
        var random = IoCManager.Resolve<IRobustRandom>();
        var singleAccentuator = new SingleAccentuator();
        return !random.Prob(chance) ? message : singleAccentuator.Accentuate(message);
    }

    public FormattedMessage MaybeAccentuate(FormattedMessage message, float chance = DefaultAccentuationChance)
    {
        var random = IoCManager.Resolve<IRobustRandom>();
        if (!random.Prob(chance))
            return message;


        var singleAccentuator = new SingleAccentuator();
        var newMessage = new FormattedMessage();

        foreach (var node in message)
        {
            if (node.Name is null && node.Value.TryGetString(out var text))
            {
                var accentedText = singleAccentuator.Accentuate(text);
                newMessage.PushTag(new MarkupNode(accentedText));
            }
            else
            {
                newMessage.PushTag(node);
            }
        }

        return newMessage;
    }
}

public sealed class SingleAccentuator: EntitySystem
{
    private readonly IReadOnlyList<IAccentSystem> _accentSystems = new List<IAccentSystem>
    {
        new OwOAccentSystem(),
        new GermanAccentSystem(),
        new RussianAccentSystem(),
        new OwOAccentSystem(),
    };

    private readonly IAccentSystem _accentSystem;

    public SingleAccentuator()
    {
        _accentSystem = GetRandomAccentSystem();
    }

    private IAccentSystem GetRandomAccentSystem()
    {
        var random = IoCManager.Resolve<IRobustRandom>();
        return random.Pick(_accentSystems);
    }

    // private Type? GetComponentType(IAccentSystem accentSystem)
    // {
    //     var accentSystemType = accentSystem.GetType();
    //     var componentType = accentSystemType.Assembly.GetTypes()
    //         .FirstOrDefault(t =>
    //             t.Name.StartsWith(accentSystemType.Name.Replace("System", "")) && t.IsSubclassOf(typeof(Component)));
    //     return componentType;
    // }

    public string Accentuate(string message)
    {
        // var componentType = GetComponentType(_accentSystem);
        // if (componentType is null)
        //     return message;
        //
        // var component = (Component)Activator.CreateInstance(componentType);
        return _accentSystem.Accentuate(message);
    }
}
