#nullable enable
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components.Nutrition
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
        private string? _meatPrototype;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _meatPrototype, "meat", null);
        }

        public bool CanDrop(CanDropEventArgs args)
        {
            return true;
        }
    }
}
