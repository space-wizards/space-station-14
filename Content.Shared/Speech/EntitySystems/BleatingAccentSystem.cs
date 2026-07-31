using System.Text.RegularExpressions;
using Content.Shared.Speech.Components;

namespace Content.Shared.Speech.EntitySystems;

public sealed partial class BleatingAccentSystem : RelayAccentSystem<BleatingAccentComponent>
{
    private static readonly Regex BleatRegex = new("([mbdlpwhrkcnytfo])([aiu])", RegexOptions.IgnoreCase);

    public override string Accentuate(string message, Entity<BleatingAccentComponent>? ent = null)
    {
        // Repeats the vowel in certain consonant-vowel pairs
        // So you taaaalk liiiike thiiiis
        return BleatRegex.Replace(message, "$1$2$2$2$2");
    }
}
