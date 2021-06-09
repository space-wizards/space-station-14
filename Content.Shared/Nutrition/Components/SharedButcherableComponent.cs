#nullable enable
using Content.Shared.DragDrop;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Nutrition.Components
{
    /// <summary>
    /// Indicates that the entity can be thrown on a kitchen spike for butchering.
    /// </summary>
    [RegisterComponent]
    public class SharedButcherableComponent : Component, IDraggable
    {
        public override string Name => "Butcherable";

        [ViewVariables]
        public string? MeatPrototype => _meatPrototype;

        [ViewVariables]
        [DataField("meat")]
        private string? _meatPrototype;

        public bool CanDrop(CanDropEvent args)
        {
            return true;
        }
    }
}
