using System.Linq;
using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
using Content.Server.Speech.Prototypes;
using Content.Shared.Speech;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Speech.EntitySystems
{
    // TODO: Code in-game languages and make this a language
    /// <summary>
    /// Replaces text in messages, either with full replacements or word replacements.
    /// </summary>
    public sealed class ReplacementAccentSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _proto = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly ILocalizationManager _loc = default!;

        private readonly Dictionary<ProtoId<ReplacementAccentPrototype>, (Regex regex, string replacement)[]>
            _cachedReplacements = new();

        public override void Initialize()
        {
            SubscribeLocalEvent<ReplacementAccentComponent, AccentGetEvent>(OnAccent);

            _proto.PrototypesReloaded += OnPrototypesReloaded;
        }

        public override void Shutdown()
        {
            base.Shutdown();

            _proto.PrototypesReloaded -= OnPrototypesReloaded;
        }

        private void OnAccent(EntityUid uid, ReplacementAccentComponent component, AccentGetEvent args)
        {
            args.Message = ApplyReplacements(args.Message, component.Accent);
        }

        /// <summary>
        ///     Attempts to apply a given replacement accent prototype to a message.
        /// </summary>
        [PublicAPI]
        public string ApplyReplacements(string message, string accent)
        {
            if (!_proto.TryIndex<ReplacementAccentPrototype>(accent, out var prototype))
                return message;

            if (!_random.Prob(prototype.ReplacementChance))
                return message;

            // Prioritize fully replacing if that exists--
            // ideally both aren't used at the same time (but we don't have a way to enforce that in serialization yet)
            if (prototype.FullReplacements != null)
            {
                return prototype.FullReplacements.Length != 0 ? Loc.GetString(_random.Pick(prototype.FullReplacements)) : "";
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
                for (int i = regex.Count(maskMessage); i > 0; i--)
                {
                    // fetch the match again as the character indices may have changed
                    Match match = regex.Match(maskMessage);
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
            if (!_cachedReplacements.TryGetValue(prototype.ID, out var replacements))
            {
                replacements = GenerateCachedReplacements(prototype);
                _cachedReplacements.Add(prototype.ID, replacements);
            }

            return replacements;
        }

        private (Regex regex, string replacement)[] GenerateCachedReplacements(ReplacementAccentPrototype prototype)
        {
            if (prototype.WordReplacements is not { } replacements)
                return [];

            return replacements.Select(kv =>
                {
                    var (first, replace) = kv;
                    var firstLoc = _loc.GetString(first);
                    var replaceLoc = _loc.GetString(replace);

                    var regex = new Regex($@"(?<![\w']){firstLoc}(?![\w'])", RegexOptions.IgnoreCase);

                    return (regex, replaceLoc);

                })
                .ToArray();
        }

        private void OnPrototypesReloaded(PrototypesReloadedEventArgs obj)
        {
            _cachedReplacements.Clear();
        }
    }
}
