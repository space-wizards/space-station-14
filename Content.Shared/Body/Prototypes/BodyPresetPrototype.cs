using System;
using System.Collections.Generic;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Body.Prototypes
{
    /// <summary>
    ///     Defines the parts used in a body.
    /// </summary>
    [Prototype("bodyPreset")]
    [Serializable, NetSerializable]
    public sealed class BodyPresetPrototype : IPrototype
    {
        [ViewVariables]
        [DataField("id", required: true)]
        public string ID { get; } = default!;

        [DataField("partIDs")]
        private Dictionary<string, string> _partIDs = new();

        [ViewVariables]
        [DataField("name")]
        public string Name { get; } = string.Empty;

        [ViewVariables]
        public Dictionary<string, string> PartIDs => new(_partIDs);
    }
}
