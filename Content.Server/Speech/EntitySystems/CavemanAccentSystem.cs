using Content.Server.Speech.Components;
using Robust.Shared.Random;
using System.Linq;
using System.Text.RegularExpressions;

namespace Content.Server.Speech.EntitySystems;

public sealed class CavemanAccentSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ReplacementAccentSystem _replacement = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CavemanAccentComponent, AccentGetEvent>(OnAccentGet);
    }

    private string Convert(string message, CavemanAccentComponent component)
    {
        string msg = _replacement.ApplyReplacements(message, "caveman");

        string[] words = msg.Split(' ');
        List<string> modifiedWords = new List<string>();

        foreach (var word in words)
        {
            string endPunctuation = "";

            for (int letterIndex = word.Length - 1; letterIndex >= 0; letterIndex--)
            {
                if (word[letterIndex] == '!' || word[letterIndex] == '?' || word[letterIndex] == '.')
                {
                    endPunctuation += word[letterIndex];
                }
                else if (char.IsLetter(word[letterIndex]))
                {
                    break;
                }
            }

            var modifiedWord = word.ToLower();

            if (IsForbidden(word)) continue;

            modifiedWord = TryRemoveForbiddenSuffix(modifiedWord);

            modifiedWord = TryRemovePunctuation(modifiedWord);

            modifiedWord = TryRemoveFancyPhonemes(modifiedWord);

            modifiedWord = TryConvertNumbers(modifiedWord);

            if (modifiedWord.Length > CavemanAccentComponent.MaxWordLength) modifiedWord = Loc.GetString(_random.Pick(CavemanAccentComponent.Grunts));

            foreach (var c in modifiedWord)
            {
                if (!char.IsLetter(c))
                {
                    modifiedWord = Loc.GetString(_random.Pick(CavemanAccentComponent.Grunts));
                    break;
                }
            }

            modifiedWord += endPunctuation;

            modifiedWords.Add(modifiedWord);
        }

        if (modifiedWords.Count == 0)
        {
            modifiedWords[^1] += Loc.GetString(_random.Pick(CavemanAccentComponent.Grunts));
        }

        return string.Join(" ", modifiedWords);
    }

    private void OnAccentGet(EntityUid uid, CavemanAccentComponent component, AccentGetEvent args)
    {
        args.Message = Convert(args.Message, component);
    }

    private bool IsForbidden(string word)
    {
        foreach (string localizationID in CavemanAccentComponent.ForbiddenWords)
        {
            string forbiddenWord = Loc.GetString(localizationID);

            if (word == forbiddenWord)
            {
                return true;
            }
        }

        return false;
    }

    private string TryRemoveForbiddenSuffix(string word)
    {
        if (word.Length < CavemanAccentComponent.MinWordLengthToRemoveForbiddenSuffix)
        {
            return word;
        }

        foreach (string localizationID in CavemanAccentComponent.ForbiddenSuffixes)
        {
            string suffix = Loc.GetString(localizationID);

            if (word.EndsWith(suffix))
            {
                return word.Substring(0, word.Length - suffix.Length);
            }
        }

        return word;
    }

    private string TryRemovePunctuation(string word)
    {
        return word.Trim(['.', ',', '!', '?', ';', ':']);
    }

    private string TryRemoveFancyPhonemes(string word)
    {
        return word;
    }

    private string TryConvertNumbers(string word)
    {
        int num;

        if (int.TryParse(word, out num))
        {
            if (num > 10)
            {
                return Loc.GetString(CavemanAccentComponent.Numbers[^1]);
            }
            else if (num <= 10 && num > 0)
            {
                return Loc.GetString(CavemanAccentComponent.Numbers[num]);
            }
            else
            {
                return Loc.GetString(CavemanAccentComponent.Numbers[0]);
            }
        }

        return word;
    }

}
