#nullable enable
using System;
using System.Collections.Generic;
using Robust.Shared.Maths;

namespace Content.Shared.Preferences.Appearance
{
    public static class HairStyles
    {
        public const string DefaultHairStyle = "HumanHairBald";
        public const string DefaultFacialHairStyle = "HumanFacialHairShaved";

        public static readonly IReadOnlyList<Color> RealisticHairColors = new List<Color>
        {
            Color.Yellow,
            Color.Black,
            Color.SandyBrown,
            Color.Brown,
            Color.Wheat,
            Color.Gray
        };

        // These comparers put the default hair style (shaved/bald) at the very top.
        // For in the hair style pickers.

        public static readonly IComparer<SpriteAccessoryPrototype> HairStyleComparer =
            Comparer<SpriteAccessoryPrototype>.Create((a, b) =>
            {
                var styleA = a.ID;
                var styleB = b.ID;
                if (styleA == DefaultHairStyle)
                {
                    return -1;
                }

                if (styleB == DefaultHairStyle)
                {
                    return 1;
                }

                return string.Compare(styleA, styleB, StringComparison.CurrentCulture);
            });

        public static readonly IComparer<SpriteAccessoryPrototype> FacialHairStyleComparer =
            Comparer<SpriteAccessoryPrototype>.Create((a, b) =>
            {
                var styleA = a.ID;
                var styleB = b.ID;

                if (styleA == DefaultFacialHairStyle)
                {
                    return -1;
                }

                if (styleB == DefaultFacialHairStyle)
                {
                    return 1;
                }

                return string.Compare(styleA, styleB, StringComparison.CurrentCulture);
            });
    }
}
