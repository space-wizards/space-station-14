using System.Linq;
using Robust.Shared.Prototypes;

namespace Content.Shared.Radio;

public sealed class SharedRadioSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public static int MinFreeFreq = 1201;
    public static int MinFreq = 1441;
    public static int MaxFreq  = 1489;
    public static int MaxFreeFreq  = 1599;

    /// <summary>
    /// Returns a valid chanel with the specified frequency or else null.
    /// </summary>
    /// <param name="freq"></param>
    /// <returns></returns>
    public RadioChannelPrototype? GetChannel(int freq) {
        var possibleChannel = _prototypeManager.EnumeratePrototypes<RadioChannelPrototype>().Where(x => x.Frequency == freq).ToList();
        return !possibleChannel.Any() ? null : possibleChannel.First();
    }

    /// <summary>
    /// Formats the frequency to the known chanel or its corresponding frequency "float" (format is 123.4 instead of 1234)
    /// </summary>
    /// <param name="freq">The frequency</param>
    /// <returns>Either the formatted frequency "Common" OR "145.9"</returns>
    public string StringifyFrequency(int freq)
    {
        var chan = GetChannel(freq);
        if (chan == null)
        {
            return Math.Floor(freq / 10F) + "." + (freq % 10);
        }
        return Loc.GetString("known-frequency-" + chan.ID);
    }

    /// <summary>
    /// Validates if the frequency you've entered is a valid one.
    /// </summary>
    /// <param name="freq"></param>
    /// <param name="free">Is this not bound to the station public channels?</param>
    /// <returns></returns>
    public int SanitizeFrequency(int freq, bool free = false)
    {
        var returnValue = free ? Math.Clamp(freq, MinFreeFreq, MaxFreeFreq) : Math.Clamp(freq, MinFreq, MaxFreq);
        if (returnValue % 2 != 1)
        {
            returnValue += 1;
        }
        return returnValue;
    }

    public bool IsOutsideFreeFreq(int freq)
    {
        return freq < MinFreq || freq > MaxFreq;
    }
}
