using System.Linq;
using System.Text.RegularExpressions;
using Content.Shared.Random.Helpers;
using Content.Shared.Speech.Components;
using Content.Shared.Speech.Prototypes;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Speech.EntitySystems;

// TODO: Code in-game languages and make this a language
/// <summary>
/// Replaces text in messages, either with full replacements or word replacements.
/// </summary>
public sealed partial class ReplacementAccentSystem : RelayAccentSystem<ReplacementAccentComponent>
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;

    private readonly Dictionary<ProtoId<ReplacementAccentPrototype>, (Regex regex, string replacement)[]>
        _cachedReplacements = new();

    public override void Initialize()
    {
        base.Initialize();

        ProtoMan.PrototypesReloaded += OnPrototypesReloaded;
    }

    public override void Shutdown()
    {
        base.Shutdown();

        ProtoMan.PrototypesReloaded -= OnPrototypesReloaded;
    }

    public override string Accentuate(string message, Entity<ReplacementAccentComponent>? ent = null)
    {
        if (ent == null)
            return message;

        return ApplyReplacements(message, ent.Value.Comp.Accent, ent.Value.Owner);
    }

    /// <summary>
    /// Applies a replacement accent to an entity.
    /// </summary>
    [PublicAPI]
    public void ApplyAccent(EntityUid uid, string accent)
    {
        var component = EnsureComp<ReplacementAccentComponent>(uid);
        component.Accent = accent;
        Dirty(uid, component);
    }

    /// <summary>
    /// Attempts to apply a given replacement accent prototype to a message.
    /// </summary>
    [PublicAPI]
    public string ApplyReplacements(string message, string accent, EntityUid? uid = null)
    {
        if (!ProtoMan.TryIndex<ReplacementAccentPrototype>(accent, out var prototype))
            return message;

        var random = uid != null
            ? SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(uid.Value))
            : _random;
        if (!random.Prob(prototype.ReplacementChance))
            return message;

        // Prioritize fully replacing if that exists--
        // ideally both aren't used at the same time (but we don't have a way to enforce that in serialization yet)
        if (prototype.FullReplacements != null)
        {
            return prototype.FullReplacements.Length != 0 ? Loc.GetString(random.Pick(prototype.FullReplacements)) : "";
        }

        // Prohibition of repeated word replacements.
        // All replaced words placed in the final message are placed here as dashes (___) with the same length.
        // The regex search goes through this buffer message, from which the already replaced words are crossed out,
        // ensuring that the replaced words cannot be replaced again.
        var maskMessage = message;

        foreach (var (regex, replace) in GetCachedReplacements(prototype))
        {
            // this is kind of slow but its not that bad
            // essentially: go over all matches, try to match capitalization where possible, then replace
            // rather than using regex.replace
            for (var i = regex.Count(maskMessage); i > 0; i--)
            {
                // fetch the match again as the character indices may have changed
                var match = regex.Match(maskMessage);
                var replacement = replace;

                // Intelligently replace capitalization
                // two cases where we will do so:
                // - the string is all upper case (just uppercase the replacement too)
                // - the first letter of the word is capitalized (common, just uppercase the first letter too)
                // any other cases are not really useful or not viable, since the match & replacement can be different
                // lengths

                // second expression here is weird--its specifically for single-word capitalization for I or A
                // dwarf expands I -> Ah, without that it would transform I -> AH
                // so that second case will only fully-uppercase if the replacement length is also 1
                if (!match.Value.Any(char.IsLower) && (match.Length > 1 || replacement.Length == 1))
                {
                    replacement = replacement.ToUpperInvariant();
                }
                else if (match.Length >= 1 && replacement.Length >= 1 && char.IsUpper(match.Value[0]))
                {
                    replacement = replacement[0].ToString().ToUpper() + replacement[1..];
                }

                // In-place replace the match with the transformed capitalization replacement
                message = message.Remove(match.Index, match.Length).Insert(match.Index, replacement);
                var mask = new string('_', replacement.Length);
                maskMessage = maskMessage.Remove(match.Index, match.Length).Insert(match.Index, mask);
            }
        }
        return message;
    }

    private (Regex regex, string replacement)[] GetCachedReplacements(ReplacementAccentPrototype prototype)
    {
        if (_cachedReplacements.TryGetValue(prototype.ID, out var replacements))
            return replacements;

        replacements = GenerateCachedReplacements(prototype);
        _cachedReplacements.Add(prototype.ID, replacements);
        return replacements;
    }

    private (Regex regex, string replacement)[] GenerateCachedReplacements(ReplacementAccentPrototype prototype)
    {
        if (prototype.WordReplacements is not { } replacements)
            return [];

        return
        [
            .. replacements.Select(kv =>
            {
                var (first, replace) = kv;
                var firstLoc = Loc.GetString(first);
                var replaceLoc = Loc.GetString(replace);

                var regex = new Regex($@"(?<![\w']){firstLoc}(?![\w'])", RegexOptions.IgnoreCase);

                return (regex, replaceLoc);

            }),
        ];
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs obj)
    {
        _cachedReplacements.Clear();
    }
}
