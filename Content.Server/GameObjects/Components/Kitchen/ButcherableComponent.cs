#nullable enable
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
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
        [DataField("meat")]
        private string? _meatPrototype;
    }
}

