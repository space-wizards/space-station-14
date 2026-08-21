using Content.Shared.Humanoid.Markings;
using Robust.Shared.Prototypes;

namespace Content.Shared.Humanoid
{
    public static class HairStyles
    {
        public static readonly ProtoId<MarkingPrototype> DefaultHairStyle = "HairBald";

        public static readonly ProtoId<MarkingPrototype> DefaultFacialHairStyle = "FacialHairShaved";

        public static readonly IReadOnlyList<Color> RealisticHairColors = new List<Color>
        {
            Color.Yellow,
            Color.Black,
            Color.SandyBrown,
            Color.Brown,
            Color.Wheat,
            Color.Gray
        };
    }
}
