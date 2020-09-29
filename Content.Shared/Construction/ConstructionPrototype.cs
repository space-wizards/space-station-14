using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.GameObjects.Components.Interactable;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.Construction
{
    [Prototype("construction")]
    public class ConstructionPrototype : IPrototype, IIndexedPrototype
    {
        private List<string> _keywords;
        private List<string> _categorySegments;

        /// <summary>
        ///     Friendly name displayed in the construction GUI.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        ///     "Useful" description displayed in the construction GUI.
        /// </summary>
        public string Description { get; private set; }

        public string Graph { get; private set; }

        /// <summary>
        ///     Texture path inside the construction GUI.
        /// </summary>
        public SpriteSpecifier Icon { get; private set; }

        /// <summary>
        ///     If you can start building or complete steps on impassable terrain.
        /// </summary>
        public bool CanBuildInImpassable { get; private set; }

        /// <summary>
        ///     A list of keywords that are used for searching.
        /// </summary>
        public IReadOnlyList<string> Keywords => _keywords;

        /// <summary>
        ///     The split up segments of the category.
        /// </summary>
        public IReadOnlyList<string> CategorySegments => _categorySegments;

        public ConstructionType Type { get; private set; }

        public string ID { get; private set; }

        public string PlacementMode { get; private set; }

        public void LoadFrom(YamlMappingNode mapping)
        {
            var ser = YamlObjectSerializer.NewReader(mapping);
            Name = ser.ReadDataField<string>("name");

            ser.DataField(this, x => x.ID, "id", string.Empty);
            ser.DataField(this, x => x.Graph, "graph", string.Empty);
            ser.DataField(this, x => x.Description, "description", string.Empty);
            ser.DataField(this, x => x.Icon, "icon", SpriteSpecifier.Invalid);
            ser.DataField(this, x => x.Type, "objectType", ConstructionType.Structure);
            ser.DataField(this, x => x.PlacementMode, "placementMode", "PlaceFree");
            ser.DataField(this, x => x.CanBuildInImpassable, "canBuildInImpassable", false);

            _keywords = ser.ReadDataField("keywords", new List<string>());
            {
                var cat = ser.ReadDataField<string>("category");
                var split = cat.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                _categorySegments = split.ToList();
            }
        }
    }

    public enum ConstructionType
    {
        Structure,
        Item,
    }
}

