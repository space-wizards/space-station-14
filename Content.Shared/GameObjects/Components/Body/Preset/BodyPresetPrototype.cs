#nullable enable
using System;
using System.Collections.Generic;
using Content.Shared.GameObjects.Components.Body.Part;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components.Body.Preset
{
    /// <summary>
    ///     Defines the <see cref="IBodyPart"/>s used in a <see cref="IBody"/>.
    /// </summary>
    [Prototype("bodyPreset")]
    [Serializable, NetSerializable]
    public class BodyPresetPrototype : IPrototype
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
