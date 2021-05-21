#nullable enable
using System.Collections.Generic;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.AI
{
    [Prototype("aiFaction")]
    public class AiFactionPrototype : IPrototype
    {
        // These are immutable so any dynamic changes aren't saved back over.
        // AiFactionSystem will just read these and then store them.
        [ViewVariables]
        [DataField("id", required: true)]
        public string ID { get; } = default!;

        [DataField("hostile")]
        public IReadOnlyList<string> Hostile { get; private set; } = new List<string>();
    }
}
