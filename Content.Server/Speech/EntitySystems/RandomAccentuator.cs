using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Speech.EntitySystems;

public static class RandomAccentuator
{
    private const float DefaultAccentuationChance = 1.0f;

    private const float DefaultReaccentuationChance = 0.5f;

    public static string MaybeAccentuate(string message, float chance = DefaultAccentuationChance, float reaccentuationChance = DefaultReaccentuationChance)
    {
        var random = IoCManager.Resolve<IRobustRandom>();
        var singleAccentuator = new SingleAccentuator();
        return !random.Prob(chance) ? message : MaybeAccentuateInternal(message, singleAccentuator, random, reaccentuationChance);
    }

    public static FormattedMessage MaybeAccentuate(FormattedMessage message, float chance = DefaultAccentuationChance, float reaccentuationChance = DefaultReaccentuationChance)
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
                var accentedText = MaybeAccentuateInternal(text, singleAccentuator, random, reaccentuationChance);
                newMessage.PushTag(new MarkupNode(accentedText));
            }
            else
            {
                newMessage.PushTag(node);
            }
        }

        return newMessage;
    }

    private static string MaybeAccentuateInternal(string message, SingleAccentuator singleAccentuator, IRobustRandom random, float reaccentuationChance)
    {
        for (var i = 0; i < 3 && random.Prob(reaccentuationChance); i++)
        {
            singleAccentuator.NextSystem();
            message = singleAccentuator.Accentuate(message);
        }

        return message;
    }
}
