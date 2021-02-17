#nullable enable
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Kitchen
{
    /// <summary>
    /// Indicates that the entity can be thrown on a kitchen spike for butchering.
    /// </summary>
    [RegisterComponent]
    public class ButcherableComponent : Component
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
    }
}

