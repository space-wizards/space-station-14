using Content.Shared.FixedPoint;
using System.Linq;
using System.Text.Json.Serialization;

namespace Content.Server.Corvax.GuideGenerator;
public sealed class ReagentEffectsEntry
{
    [JsonPropertyName("rate")]
    public FixedPoint2 MetabolismRate { get; } = FixedPoint2.New(0.5f);

    [JsonPropertyName("effects")]
    public List<ReagentEffectEntry> Effects { get; } = new();

    public ReagentEffectsEntry(Shared.Chemistry.Reagent.ReagentEffectsEntry proto)
    {
        MetabolismRate = proto.MetabolismRate;
        Effects = proto.Effects.Select(x => new ReagentEffectEntry(x)).ToList();
    }

}
