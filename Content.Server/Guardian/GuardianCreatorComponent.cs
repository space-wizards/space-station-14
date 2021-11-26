using Content.Server.Construction.Components;
using Content.Server.Power.Components;
using Content.Shared.Computer;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Guardian
{
    /// <summary>
    /// Allows user to create a guardian link with a specific entity at a certain max distance
    /// </summary>
    [RegisterComponent]
    public class GuardianCreatorComponent : Component
    {
        public override string Name => "GuardianCreator";

        /// <summary>
        /// Counts as spent upon exhausting the injection
        /// </summary>
        public bool Used = false;

        /// <summary>
        /// The prototype of the guardian entity which will be created
        /// </summary>
        [ViewVariables] [DataField("guardianID")] public string GuardianType { get; set; } = default!;
    }
}
