using System;
using System.Collections.Generic;
using Content.Shared.GameObjects.Components.Body.Part;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.GameObjects.Components.Body.Preset
{
    /// <summary>
    ///     Defines the <see cref="IBodyPart"/>s used in a <see cref="IBody"/>.
    /// </summary>
    [Prototype("bodyPreset")]
    [Serializable, NetSerializable]
    public class BodyPresetPrototype : IPrototype, IIndexedPrototype
    {
        [DataField("id")]
        private string _id;
        [DataField("name")]
        private string _name;
        [DataField("partIDs")]
        private Dictionary<string, string> _partIDs = new();

        [ViewVariables] public string ID => _id;

        [ViewVariables] public string Name => _name;

        [ViewVariables] public Dictionary<string, string> PartIDs => new(_partIDs);
    }
}
