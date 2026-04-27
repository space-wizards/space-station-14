using System.Linq;
using System.Numerics;
using Content.Shared.Body;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Humanoid;

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class HumanoidCharacterAppearance : IEquatable<HumanoidCharacterAppearance>
{
    [DataField]
    public Color EyeColor { get; set; } = Color.Black;

    [DataField]
    public Color SkinColor { get; set; } = Color.FromHsv(new Vector4(0.07f, 0.2f, 1f, 1f));

    [DataField]
    public Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> Markings
    {
        get;
        set;
    } = new();

    public HumanoidCharacterAppearance(
        Color eyeColor,
        Color skinColor,
        Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> markings)
    {
        EyeColor = ClampColor(eyeColor);
        SkinColor = ClampColor(skinColor);
        Markings = markings;
    }

    public HumanoidCharacterAppearance(HumanoidCharacterAppearance other) :
        this(other.EyeColor, other.SkinColor, new(other.Markings))
    {
    }

    public HumanoidCharacterAppearance WithEyeColor(Color newColor)
    {
        return new(newColor, SkinColor, Markings);
    }

    public HumanoidCharacterAppearance WithSkinColor(Color newColor)
    {
        return new(EyeColor, newColor, Markings);
    }

    public HumanoidCharacterAppearance WithMarkings(
        Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> newMarkings)
    {
        return new(EyeColor, SkinColor, newMarkings);
    }

    public static HumanoidCharacterAppearance DefaultWithSpecies(ProtoId<SpeciesPrototype> species, Sex sex)
    {
        var protoMan = IoCManager.Resolve<IPrototypeManager>();
        var speciesPrototype = protoMan.Index<SpeciesPrototype>(species);
        var skinColoration = protoMan.Index(speciesPrototype.SkinColoration).Strategy;
        var skinColor = skinColoration.InputType switch
        {
            SkinColorationStrategyInput.Unary => skinColoration.FromUnary(speciesPrototype.DefaultHumanSkinTone),
            SkinColorationStrategyInput.Color => skinColoration.ClosestSkinColor(speciesPrototype.DefaultSkinTone),
            _ => skinColoration.ClosestSkinColor(speciesPrototype.DefaultSkinTone),
        };

        var appearance = new HumanoidCharacterAppearance(
            Color.Black,
            skinColor,
            new()
        );
        return EnsureValid(appearance, species, sex);
    }

    private static IReadOnlyList<Color> _realisticEyeColors = new List<Color>
    {
        Color.Brown,
        Color.Gray,
        Color.Azure,
        Color.SteelBlue,
        Color.Black
    };

    public static HumanoidCharacterAppearance Random(string species, Sex sex)
    {
        var random = IoCManager.Resolve<IRobustRandom>();
        var markingManager = IoCManager.Resolve<MarkingManager>();
        var protoMan = IoCManager.Resolve<IPrototypeManager>();
        var speciesPrototype = protoMan.Index<SpeciesPrototype>(species);

        Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> compiledMarkings =
            new();

        var skinType = speciesPrototype.SkinColoration;
        var strategy = protoMan.Index(skinType).Strategy;
        //hair colors can go pretty wild
        var hairColour = new Color(random.NextFloat(1),
            random.NextFloat(1),
            random.NextFloat(1),
            1);

        //build a color pallet for all organic parts, which should be a skin color
        var organicColors = Enumerable.Range(0, 255)
            .Select(e => strategy.InputType switch
            {
                SkinColorationStrategyInput.Unary => strategy.FromUnary(random.NextFloat(0f, 100f)),
                SkinColorationStrategyInput.Color => strategy.ClosestSkinColor(new Color(random.NextFloat(1),
                    random.NextFloat(1),
                    random.NextFloat(1),
                    1)),
                _ => strategy.ClosestSkinColor(new Color(random.NextFloat(1), random.NextFloat(1), random.NextFloat(1), 1)),
            })
            .ToArray();
        //most likely list of simple physical traits.
        HumanoidVisualLayers[] organicLayerFilter =
        [
        HumanoidVisualLayers.Hair,
        HumanoidVisualLayers.Tail,
        HumanoidVisualLayers.FacialHair,
        HumanoidVisualLayers.Fire,
        HumanoidVisualLayers.Snout,
        ];

        //build organ for organ (in random order)
        var organList = markingManager.GetOrgans(species).ToList();
        random.Shuffle(organList);
        foreach (var organ in organList)
        {
            //get the marking data for that organ
            if (!markingManager.TryGetMarkingData(organ.Value, out var organMarkingData))
                continue;
            //extract the group based on the organ
            var group = protoMan.Index<MarkingsGroupPrototype>(organMarkingData.Value.Group.Id);
            // setup an empty dictionary of layers
            compiledMarkings[organ.Key] = new();
            //layer for layer (in random order)
            var layers = organMarkingData.Value.Layers.ToList();
            random.Shuffle(layers);
            foreach (var layer in layers)
            {
                //only randomize physical traits.
                if(!organicLayerFilter.Contains(layer))
                    continue;
                //get all markings for that layer, sex, group and flatten to markings.
                var markings =
                    markingManager.MarkingsByLayerAndGroupAndSex(layer, organMarkingData.Value.Group, sex)
                        .Select(e => e.Value.AsMarking())
                        .ToArray();
                //skip if no matches
                if(markings.Length==0)
                    continue;
                //check restrictions for the layer
                int limitOfMarking;
                if (!group.Limits.TryGetValue(layer, out var limits))
                {
                    //make up to as many marking as we have options
                    limitOfMarking = markings.Length;
                    //flip coin to see if we skip.
                    if(limitOfMarking==1 && random.NextDouble() < 0.5)
                        continue;
                }
                else
                {
                    limitOfMarking = limits.Limit;
                    //blatant skip chance unless required.
                    if (!limits.Required && limitOfMarking==1 && random.NextDouble() < 0.5)
                             continue;
                }

                //pick random feature list within limit
                compiledMarkings[organ.Key][layer] = Enumerable.Range(0,limitOfMarking==1?1:random.Next(limitOfMarking))
                    .Select(e =>
                    {
                       // return random.Pick(markings);
                              var baseMarking = random.Pick(markings);
                              if (layer is HumanoidVisualLayers.Hair or HumanoidVisualLayers.FacialHair)
                              {
                                  return baseMarking.WithColor(hairColour);
                              }
                              for (var i = 0; i < baseMarking.MarkingColors.Count&&i<organicColors.Length; i++)
                              {
                               baseMarking= baseMarking.WithColorAt(i, organicColors[i]);
                              }
                              return baseMarking;
                              //    return baseMarking.WithColor(color);
                    })
                    .ToList();
            }
        }

        var newEyeColor = random.Pick(_realisticEyeColors);



        var newSkinColor = strategy.InputType switch
        {
            SkinColorationStrategyInput.Unary => strategy.FromUnary(random.NextFloat(0f, 100f)),
            SkinColorationStrategyInput.Color => strategy.ClosestSkinColor(new Color(random.NextFloat(1),
                random.NextFloat(1),
                random.NextFloat(1),
                1)),
            _ => strategy.ClosestSkinColor(new Color(random.NextFloat(1), random.NextFloat(1), random.NextFloat(1), 1)),
        };


        // Safety step. Most systems which called Random() also called this, and not doing so caused issues with markings.
        // In the future it could *maybe* be removed, but it's probably worth the extra CPU cycles to validate this info.

        return EnsureValid(
            new HumanoidCharacterAppearance(newEyeColor, newSkinColor, compiledMarkings),
            species,
            sex);
    }

    public static Color ClampColor(Color color)
    {
        return new(color.RByte, color.GByte, color.BByte);
    }

    public static HumanoidCharacterAppearance EnsureValid(HumanoidCharacterAppearance appearance,
        ProtoId<SpeciesPrototype> species,
        Sex sex)
    {
        var eyeColor = ClampColor(appearance.EyeColor);

        var proto = IoCManager.Resolve<IPrototypeManager>();
        var markingManager = IoCManager.Resolve<MarkingManager>();

        var skinColor = appearance.SkinColor;
        var validatedMarkings = appearance.Markings.ShallowClone();

        if (proto.TryIndex(species, out var speciesProto))
        {
            var strategy = proto.Index(speciesProto.SkinColoration).Strategy;
            var organs = markingManager.GetOrgans(species);
            skinColor = strategy.EnsureVerified(skinColor);

            foreach (var (organ, markings) in appearance.Markings)
            {
                if (!organs.ContainsKey(organ))
                    validatedMarkings.Remove(organ);
            }

            foreach (var (organ, organProtoID) in organs)
            {
                if (!markingManager.TryGetMarkingData(organProtoID, out var organData))
                {
                    validatedMarkings.Remove(organ);
                    continue;
                }

                var actualMarkings = appearance.Markings.GetValueOrDefault(organ)?.ShallowClone() ?? [];

                markingManager.EnsureValidColors(actualMarkings);
                markingManager.EnsureValidGroupAndSex(actualMarkings, organData.Value.Group, sex);
                markingManager.EnsureValidLayers(actualMarkings, organData.Value.Layers);
                markingManager.EnsureValidLimits(actualMarkings,
                    organData.Value.Group,
                    organData.Value.Layers,
                    skinColor,
                    eyeColor);

                validatedMarkings[organ] = actualMarkings;
            }
        }

        return new HumanoidCharacterAppearance(
            eyeColor,
            skinColor,
            validatedMarkings);
    }

    public bool Equals(HumanoidCharacterAppearance? other)
    {
        if (ReferenceEquals(null, other))
            return false;
        if (ReferenceEquals(this, other))
            return true;
        return EyeColor.Equals(other.EyeColor) &&
               SkinColor.Equals(other.SkinColor) &&
               MarkingManager.MarkingsAreEqual(Markings, other.Markings);
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is HumanoidCharacterAppearance other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(EyeColor, SkinColor, Markings);
    }

    public HumanoidCharacterAppearance Clone()
    {
        return new(this);
    }
}
