using System.Text.RegularExpressions;
using Content.Shared.Speech.Components.AccentComponents;

namespace Content.Shared.Speech.EntitySystems.AccentSystems;

public sealed class BleatingAccentSystem : AccentSystem<BleatingAccentComponent>
{
    private static readonly Regex BleatRegex = new("([mbdlpwhrkcnytfo])([aiu])", RegexOptions.IgnoreCase);

    public override string Accentuate(Entity<BleatingAccentComponent>? entity, string message)
    {
        // Repeats the vowel in certain consonant-vowel pairs
        // So you taaaalk liiiike thiiiis
        return BleatRegex.Replace(message, "$1$2$2$2$2");
    }
}
