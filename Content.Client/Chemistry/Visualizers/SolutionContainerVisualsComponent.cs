using System;
using Content.Shared.Chemistry;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Client.Chemistry.Visualizers
{
    [RegisterComponent]
    public sealed class SolutionContainerVisualsComponent : Component
    {
        [DataField("maxFillLevels")]
        public int MaxFillLevels = 0;
        [DataField("fillBaseName")]
        public string? FillBaseName = null;
        [DataField("layer")]
        public SolutionContainerLayers Layer = SolutionContainerLayers.Fill;
        [DataField("changeColor")]
        public bool ChangeColor = true;
        [DataField("emptySpriteName")]
        public string? EmptySpriteName = null;
        [DataField("emptySpriteColor")]
        public Color EmptySpriteColor = Color.White;
    }
}
