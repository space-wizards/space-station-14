using System.Linq;
using System.Text.RegularExpressions;
using Content.Shared.Speech.EntitySystems;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Shared.Speech.Accents;

public sealed class ParrotAccent : IAccent
{
    public string Name { get; } = "Parrot";

    [Dependency] private readonly IEntitySystemManager _entSys = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    private SharedReplacementAccentSystem _replacement = default!;

    private static readonly Regex WordCleanupRegex = new Regex("[^A-Za-z0-9 -]");

    public string Accentuate(string message, Dictionary<string, MarkupParameter> attributes, int randomSeed)
    {
        IoCManager.InjectDependencies(this);
        _replacement = _entSys.GetEntitySystem<SharedReplacementAccentSystem>();
        long squawkChance = 1;
        long repeatChance = 1;
        long minLength = 1;
        string? squawkPrefix = null;
        var squawkAmount = 1;

        if (attributes.TryGetValue("squawkChance", out var squawkChanceParameter))
            squawkChance = squawkChanceParameter.LongValue!.Value;
        if (attributes.TryGetValue("repeatChance", out var repeatChanceParameter))
            repeatChance = repeatChanceParameter.LongValue!.Value;
        if (attributes.TryGetValue("minLength", out var minLengthParameter))
            minLength = minLengthParameter.LongValue!.Value;
        if (attributes.TryGetValue("squawkPrefix", out var squawkPrefixParameter))
            squawkPrefix = squawkPrefixParameter.StringValue;
        if (attributes.TryGetValue("squawkAmount", out var squawkAmountParameter))
            squawkAmount = (int) squawkAmountParameter.LongValue!.Value;

        // Sometimes repeat the longest word at the end of the message, after a squawk! SQUAWK! Sometimes!
        if (_random.Prob(repeatChance))
        {
            // Don't count non-alphanumeric characters as parts of words
            var cleaned = WordCleanupRegex.Replace(message, string.Empty);
            // Split on whitespace and favor words towards the end of the message
            var words = cleaned.Split(null).Reverse();
            // Find longest word
            var longest = words.MaxBy(word => word.Length);
            if (longest?.Length >= minLength)
            {
                message = EnsurePunctuation(message);

                // Capitalize the first letter of the repeated word
                longest = string.Concat(longest[0].ToString().ToUpper(), longest.AsSpan(1));

                message = string.Format("{0} {1} {2}!", message, GetRandomSquawk(squawkPrefix, squawkAmount), longest);
                return message; // No more changes, or it's too much
            }
        }

        if (_random.Prob(squawkChance))
        {
            // AWWK! Sometimes add a squawk at the begining of the message
            message = string.Format("{0} {1}", GetRandomSquawk(squawkPrefix, squawkAmount), message);
        }
        else
        {
            // Otherwise add a squawk at the end of the message! RAWWK!
            message = EnsurePunctuation(message);
            message = string.Format("{0} {1}", message, GetRandomSquawk(squawkPrefix, squawkAmount));
        }

        return message;
    }

    /// <summary>
    /// Adds a "!" to the end of the string, if there isn't already a sentence-ending punctuation mark.
    /// </summary>
    private string EnsurePunctuation(string message)
    {
        if (!message.EndsWith('!') && !message.EndsWith('?') && !message.EndsWith('.'))
            return message + '!';
        return message;
    }

    /// <summary>
    /// Returns a random, localized squawk sound.
    /// </summary>
    private string GetRandomSquawk(string? prefix, int amount)
    {
        if (prefix != null)
            return Loc.GetString(prefix + _random.Next(1, amount));
        return "";
    }

    public void GetAccentData(ref AccentGetEvent ev, Component c)
    {
        if (c is ParrotAccentComponent comp)
        {
            ev.Accents.Add(
                Name,
                new Dictionary<string, MarkupParameter>()
                {
                    { "squawkChance", new MarkupParameter((long) comp.SquawkPrefixChance) },
                    { "repeatChance", new MarkupParameter((long) comp.LongestWordRepeatChance) },
                    { "minLength", new MarkupParameter((long) comp.LongestWordMinLength) },
                    { "squawkPrefix", new MarkupParameter(comp.SquawkPrefix) },
                    { "squawkAmount", new MarkupParameter((long) comp.LongestWordMinLength) },
                });
        }
    }
}
