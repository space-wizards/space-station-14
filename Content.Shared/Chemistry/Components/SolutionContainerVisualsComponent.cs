using Content.Shared.Hands.Components;
using Robust.Shared.Utility;

namespace Content.Shared.Chemistry.Components
{
    [RegisterComponent]
    public sealed partial class SolutionContainerVisualsComponent : Component
    {
        [DataField]
        public int MaxFillLevels = 0;
        [DataField]
        public string? FillBaseName = null;
        [DataField]
        public SolutionContainerLayers Layer = SolutionContainerLayers.Fill;
        [DataField]
        public SolutionContainerLayers BaseLayer = SolutionContainerLayers.Base;
        [DataField]
        public SolutionContainerLayers OverlayLayer = SolutionContainerLayers.Overlay;
        [DataField]
        public bool ChangeColor = true;
        [DataField]
        public string? EmptySpriteName = null;
        [DataField]
        public Color EmptySpriteColor = Color.White;
        [DataField]
        public bool Metamorphic = false;
        [DataField]
        public SpriteSpecifier? MetamorphicDefaultSprite;
        [DataField]
        public LocId MetamorphicNameFull = "transformable-container-component-glass";

        /// <summary>
        /// Which solution of the SolutionContainerManagerComponent to represent.
        /// If not set, will work as default.
        /// </summary>
        [DataField]
        public string? SolutionName;

        [DataField]
        public string InitialName = string.Empty;

        [DataField]
        public string InitialDescription = string.Empty;

        /// <summary>
        /// Optional in-hand visuals to to show someone is holding a filled beaker/jug/etc.
        /// </summary>
        [DataField]
        public string? InHandsFillBaseName = null;

        /// <summary>
        /// A separate max fill levels for in-hands (to reduce number of sprites needed)
        /// </summary>
        [DataField]
        public int InHandsMaxFillLevels = 0;

        /// <summary>
        /// Optional equipped visuals to show someone is wearing a something with a filled container.
        /// </summary>
        [DataField]
        public string? EquippedFillBaseName = null;

        /// <summary>
        /// A separate max fill levels for equipped items (to reduce number of sprites needed)
        /// </summary>
        [DataField]
        public int EquippedMaxFillLevels = 0;
    }
}
