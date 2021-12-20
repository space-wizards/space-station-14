using System.Collections.Generic;
using System.Linq;
using Content.Server.Body.Components;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Newtonsoft.Json;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.GuideGenerator;

public class ReagentEntry
{
    [JsonProperty("id")]
    public string Id { get; }

    [JsonProperty("name")]
    public string Name { get; }

    [JsonProperty("group")]
    public string Group { get; }

    [JsonProperty("desc")]
    public string Description { get; }

    [JsonProperty("physicalDesc")]
    public string PhysicalDescription { get; }

    [JsonProperty("color")]
    public string SubstanceColor { get; }

    [JsonProperty("recipes")]
    public List<string> Recipes { get; } = new();

    [JsonProperty("metabolisms")]
    public Dictionary<string, ReagentEffectsEntry>? Metabolisms { get; }

    public ReagentEntry(ReagentPrototype proto)
    {
        Id = proto.ID;
        Name = proto.Name;
        Group = proto.Group;
        Description = proto.Description;
        PhysicalDescription = proto.PhysicalDescription;
        SubstanceColor = proto.SubstanceColor.ToHex();
        Metabolisms = proto.Metabolisms;
    }
}

public class ReactionEntry
{
    [JsonProperty("id")]
    public string Id { get; }

    [JsonProperty("name")]
    public string Name { get; }

    [JsonProperty("reactants")]
    public Dictionary<string, ReactantEntry> Reactants { get; }

    [JsonProperty("products")]
    public Dictionary<string, float> Products { get; }

    [JsonProperty("effects")]
    public List<ReagentEffect> Effects { get; }

    public ReactionEntry(ReactionPrototype proto)
    {
        Id = proto.ID;
        Name = proto.Name;
        Reactants =
            proto.Reactants
                .Select(x => KeyValuePair.Create(x.Key, new ReactantEntry(x.Value.Amount.Float(), x.Value.Catalyst)))
                .ToDictionary(x => x.Key, x => x.Value);
        Products =
            proto.Products
                .Select(x => KeyValuePair.Create(x.Key, x.Value.Float()))
                .ToDictionary(x => x.Key, x => x.Value);
        Effects = proto.Effects;
    }
}

public class ReactantEntry
{
    [JsonProperty("amount")]
    public float Amount { get; }

    [JsonProperty("catalyst")]
    public bool Catalyst { get; }

    public ReactantEntry(float amnt, bool cata)
    {
        Amount = amnt;
        Catalyst = cata;
    }
}
