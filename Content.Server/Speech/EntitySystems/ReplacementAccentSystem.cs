using System.Linq;
using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
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

        public override void Initialize()
        {
            SubscribeLocalEvent<ReplacementAccentComponent, AccentGetEvent>(OnAccent);
        }

        private void OnAccent(EntityUid uid, ReplacementAccentComponent component, AccentGetEvent args)
        {
            args.Message = ApplyReplacements(uid, args.Message, component.Accent);
        }

        /// <summary>
        ///     Attempts to apply a given replacement accent prototype to a message.
        /// </summary>
        [PublicAPI]
        public string ApplyReplacements(EntityUid uid, string message, string accent)
        {
            if (!_proto.TryIndex<ReplacementAccentPrototype>(accent, out var prototype))
                return message;

            // Prioritize fully replacing if that exists--
            // ideally both aren't used at the same time (but we don't have a way to enforce that in serialization yet)
            if (prototype.FullReplacements != null)
            {
                return prototype.FullReplacements.Length != 0 ? Loc.GetString(_random.Pick(prototype.FullReplacements)) : "";
            }

            if (prototype.WordReplacements == null)
                return message;

            foreach (var (first, replace) in prototype.WordReplacements)
            {
                var f = _loc.GetString(first);
                var r = _loc.GetString(replace);
                // this is kind of slow but its not that bad
                // essentially: go over all matches, try to match capitalization where possible, then replace
                // rather than using regex.replace
                foreach (Match match in Regex.Matches(message, $@"(?<!\w){f}(?!\w)", RegexOptions.IgnoreCase))
                {
                    // Intelligently replace capitalization
                    var replacement = r;

                    if (!match.Value.Any(char.IsLower))
                    {
                        replacement = replacement.ToUpperInvariant();
                    }
                    else if (match.Length >= 1 && replacement.Length >= 1 && char.IsUpper(match.Value[0]))
                    {
                        replacement = replacement[0].ToString().ToUpper() + replacement[1..];
                    }

                    // In-place replace the match with the transformed capitalization replacement
                    message = message.Remove(match.Index, match.Length).Insert(match.Index, replacement);
                }
            }

            return message;
        }
    }
}
