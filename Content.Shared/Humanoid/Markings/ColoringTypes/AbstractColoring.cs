using Content.Shared.Database;
using JetBrains.Annotations;

namespace Content.Shared.Humanoid.Markings
{
    /// <summary>
    ///     An abstract class for coloring types
    /// </summary>
    [ImplicitDataDefinitionForInheritors]
    [MeansImplicitUse]
    public abstract class LayerColoring
    {
        private protected string _id => this.GetType().Name;

        /// <summary>
        ///     Makes output color negative
        /// </summary>
        [DataField("negative")]
        public virtual bool Negative { get; } = false;
        
        /// <summary>
        ///     Color that will be used if coloring type will return nil
        /// </summary>
        [DataField("fallbackColor")]
        public Color FallbackColor = Color.White;

        public abstract Color? GetNullableColor(Color? skin, Color? eyes, MarkingSet markingSet);

        public Color GetColor(Color? skin, Color? eyes, MarkingSet markingSet)
        {
            var color = GetNullableColor(skin, eyes, markingSet) ?? FallbackColor;

            // Negative color
            if (Negative)
            {
                color.R = 1f-color.R;
                color.G = 1f-color.G;
                color.B = 1f-color.B;
            }
            return color;
        }
    }
}
