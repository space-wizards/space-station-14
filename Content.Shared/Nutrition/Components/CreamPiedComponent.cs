using System;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Nutrition.Components
{
    [Friend(typeof(SharedCreamPieSystem))]
    [RegisterComponent]
    public class CreamPiedComponent : Component
    {
        public override string Name => "CreamPied";

        [ViewVariables]
        public bool CreamPied { get; set; } = false;
    }

    [Serializable, NetSerializable]
    public enum CreamPiedVisuals
    {
        Creamed,
    }
}
