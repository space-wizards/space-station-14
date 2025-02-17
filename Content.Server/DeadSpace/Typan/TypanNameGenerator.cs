// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Server.Maps.NameGenerators;
using JetBrains.Annotations;
using Robust.Shared.Random;

namespace Content.Server.DeadSpace.Typan;

[UsedImplicitly]
public sealed partial class TypanNameGenerator : StationNameGenerator
{
    private string Prefix => "ННКСС";
    private string[] SuffixCodes => new []{ "S" };

    public override string FormatName(string input)
    {
        var random = IoCManager.Resolve<IRobustRandom>();

        // No way in hell am I writing custom format code just to add nice names. You can live with {0}
        return string.Format(input, $"{Prefix}", $"{random.Pick(SuffixCodes)}-{random.Next(0, 999):D3}");
    }
}
