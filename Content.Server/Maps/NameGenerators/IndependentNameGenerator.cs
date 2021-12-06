using System.Linq;
using JetBrains.Annotations;
using Robust.Shared.IoC;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Maps.NameGenerators;

[UsedImplicitly]
public class IndependentNameGenerator : GameMapNameGenerator
{
    /// <summary>
    ///     Where the map comes from. Should be a two or three letter code, for example "VG" for Packedstation.
    /// </summary>
    [DataField("prefixCreator")] public string PrefixCreator = default!;

    private string Prefix => "SOL";

    private static char[] _letters = "ABCDEFGHIJKLMOPQRSTUVWXYZ".ToArray(); // All letters EXCEPT 'N'.

    public override string FormatName(string input)
    {
        var random = IoCManager.Resolve<IRobustRandom>();

        // No way in hell am I writing custom format code just to add nice names. You can live with {0}
        return string.Format(input, $"{random.Pick(_letters)}{random.Pick(_letters)}{PrefixCreator}", $"{random.Pick(_letters)}{random.Pick(_letters)}-{random.Next(0, 999):D3}");
    }
}
