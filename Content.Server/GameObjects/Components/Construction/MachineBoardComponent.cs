using System.Collections.Generic;
using Content.Server.Construction;
using Content.Shared.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Construction
{
    [RegisterComponent]
    public class MachineBoardComponent : Component
    {
        public override string Name => "MachineBoard";

        [ViewVariables]
        private Dictionary<MachinePart, int> _requirements;

        [ViewVariables]
        private Dictionary<StackType, int> _materialRequirements;

        [ViewVariables]
        private Dictionary<string, int> _componentRequirements;

        /// <summary>
        ///     So, what happens if you spawn a machine from the entity spawning menu?
        ///     It should probably have all parts, including the component parts...
        ///     This is where this fancy little dictionary comes in!
        ///     This maps component name types to entity prototype IDs to be used as defaults.
        /// </summary>
        [ViewVariables]
        private Dictionary<string, string> _componentDefaults;

        [ViewVariables(VVAccess.ReadWrite)]
        public string Prototype { get; private set; }
        public IReadOnlyDictionary<MachinePart, int> Requirements => _requirements;
        public IReadOnlyDictionary<StackType, int> MaterialRequirements => _materialRequirements;
        public IReadOnlyDictionary<string, int> ComponentRequirements => _componentRequirements;
        public IReadOnlyDictionary<string, string> ComponentDefaults => _componentDefaults;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(this, x => x.Prototype, "prototype", null);
            serializer.DataField(ref _requirements, "requirements", new Dictionary<MachinePart, int>());
            serializer.DataField(ref _materialRequirements, "materialRequirements", new Dictionary<StackType, int>());
            serializer.DataField(ref _componentRequirements, "componentRequirements", new Dictionary<string, int>());
            serializer.DataField(ref _componentDefaults, "componentDefaults", new Dictionary<string, string>());
        }
    }
}
