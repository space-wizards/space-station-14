using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Speech.EntitySystems;

public static class RandomAccentuator
{
    private const float DefaultAccentuationChance = 0.5f;

    private const float DefaultReaccentuationChance = 0.5f;

    private const float MaxReaccentuations = 4;

    public static string MaybeAccentuate(string message,
        float chance = DefaultAccentuationChance,
        float reaccentuationChance = DefaultReaccentuationChance)
    {
        var random = IoCManager.Resolve<IRobustRandom>();
        var singleAccentuator = new SingleAccentuator();
        return random.Prob(chance)
            ? MaybeAccentuateDirect(message, singleAccentuator, random, reaccentuationChance)
            : message;
    }

    public static FormattedMessage MaybeAccentuate(FormattedMessage message,
        float chance = DefaultAccentuationChance,
        float reaccentuationChance = DefaultReaccentuationChance)
    {
        var random = IoCManager.Resolve<IRobustRandom>();
        var singleAccentuator = new SingleAccentuator();
        var newMessage = new FormattedMessage();

        foreach (var node in message)
        {
            if (random.Prob(chance) && node.Name is null && node.Value.TryGetString(out var text) &&
                !string.IsNullOrWhiteSpace(text))
            {
                var accentedText = MaybeAccentuateDirect(text, singleAccentuator, random, reaccentuationChance);
                newMessage.PushTag(new MarkupNode(accentedText));
            }
            else
            {
                newMessage.PushTag(node);
            }
        }

        return newMessage;
    }

    private static string MaybeAccentuateDirect(string message,
        SingleAccentuator singleAccentuator,
        IRobustRandom random,
        float reaccentuationChance)
    {
        for (var i = 0; i < MaxReaccentuations; i++)
        {
            if (i > 0 && !random.Prob(reaccentuationChance))
                continue;

            singleAccentuator.NextSystem();
            message = singleAccentuator.Accentuate(message);
        }

        return message;
    }
}
