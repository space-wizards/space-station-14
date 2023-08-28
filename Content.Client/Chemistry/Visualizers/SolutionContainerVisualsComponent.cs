using System;
using Content.Shared.Chemistry;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Content.Client.Chemistry.Visualizers
{
    [RegisterComponent]
    public sealed partial class SolutionContainerVisualsComponent : Component
    {
        [DataField("maxFillLevels")]
        public int MaxFillLevels = 0;
        [DataField("fillBaseName")]
        public string? FillBaseName = null;
        [DataField("layer")]
        public SolutionContainerLayers FillLayer = SolutionContainerLayers.Fill;
        [DataField("baseLayer")]
        public SolutionContainerLayers BaseLayer = SolutionContainerLayers.Base;
        [DataField("overlayLayer")]
        public SolutionContainerLayers OverlayLayer = SolutionContainerLayers.Overlay;
        [DataField("changeColor")]
        public bool ChangeColor = true;
        [DataField("emptySpriteName")]
        public string? EmptySpriteName = null;
        [DataField("emptySpriteColor")]
        public Color EmptySpriteColor = Color.White;
        [DataField("metamorphic")]
        public bool Metamorphic = false;
        [DataField("metamorphicDefaultSprite")]
        public SpriteSpecifier? MetamorphicDefaultSprite;
        [DataField("metamorphicNameFull")]
        public string MetamorphicNameFull = "transformable-container-component-glass";

        /// <summary>
        /// Which solution of the SolutionContainerManagerComponent to represent.
        /// If not set, will work as default.
        /// </summary>
        [DataField("solutionName")]
        public string? SolutionName;

        [DataField("initialName")]
        public string InitialName = string.Empty;

        [DataField("initialDescription")]
        public string InitialDescription = string.Empty;
    }
}
