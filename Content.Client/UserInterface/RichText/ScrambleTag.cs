using JetBrains.Annotations;
using Content.Shared.Utility;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface.RichText;

/// <summary>
/// Adds a specified length of random characters that scramble at a set rate.
/// </summary>
[UsedImplicitly]
public sealed partial class ScrambleTag : IMarkupTagHandler
{
    [Dependency] private IGameTiming _timing = default!;

    private const int MaxScrambleLength = 32;

    public string Name => "scramble";

    public string TextBefore(MarkupNode node)
    {
        if (!node.Attributes.TryGetValue("rate", out var rateParam) ||
            !rateParam.TryGetLong(out var rate) ||
            !node.Attributes.TryGetValue("length", out var lengthParam) ||
            !lengthParam.TryGetLong(out var length) ||
            !node.Attributes.TryGetValue("chars", out var charsParam) ||
            !charsParam.TryGetString(out var chars))
            return string.Empty;

        var seed = (int) (_timing.CurTime.TotalMilliseconds / rate);
        var realLength = MathF.Min(length.Value, MaxScrambleLength);
        return ScrambleUtility.Generate((int) realLength, chars, seed + node.GetHashCode());
    }
}
