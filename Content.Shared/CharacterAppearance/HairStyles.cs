#nullable enable
using System;
using System.Collections.Generic;
using Robust.Shared.Maths;

namespace Content.Shared.Preferences.Appearance
{
    public static class HairStyles
    {
        public const string DefaultHairStyle = "HairBald";
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

        // These comparers put the default hair style (shaved/bald) at the very top.
        // For in the hair style pickers.

        public static readonly IComparer<SpriteAccessoryPrototype> SpriteAccessoryComparer =
            Comparer<SpriteAccessoryPrototype>.Create((a, b) =>
            {
                var cmp = -a.Priority.CompareTo(b.Priority);
                if (cmp != 0)
                    return cmp;

                return string.Compare(a.Name, b.Name, StringComparison.CurrentCulture);
            });
    }
}
