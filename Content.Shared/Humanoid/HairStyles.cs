using Content.Shared.Humanoid.Markings;

namespace Content.Shared.Humanoid
{
    public static class HairStyles
    {
        [ValidatePrototypeId<MarkingPrototype>]
        public const string DefaultHairStyle = "HairBald";

        [ValidatePrototypeId<MarkingPrototype>]
        public const string DefaultFacialHairStyle = "FacialHairShaved";

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
