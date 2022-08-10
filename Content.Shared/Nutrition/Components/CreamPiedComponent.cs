using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Serialization;

namespace Content.Shared.Nutrition.Components
{
    [Access(typeof(SharedCreamPieSystem))]
    [RegisterComponent]
    public sealed class CreamPiedComponent : Component
    {
        [ViewVariables]
        public bool CreamPied { get; set; } = false;
    }

    [Serializable, NetSerializable]
    public enum CreamPiedVisuals
    {
        Creamed,
    }
}
