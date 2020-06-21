using System;
using Content.Shared.GameObjects.Components.HUD.Hotbar;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using YamlDotNet.RepresentationModel;

namespace Content.Client.GameObjects.Components.HUD.Hotbar
{
    [Serializable, Prototype("hotbarAction")]
    public class HotbarActionPrototype : IPrototype, IIndexedPrototype
    {
        private string _id;
        private string _name;
        private string _description;
        private string _texturePath;
        private HotbarActionId _hotbarActionId;

        public string ID => _id;

        /// <summary>
        ///     HotbarAction name.
        /// </summary>
        [ViewVariables]
        public string Name => _name;

        /// <summary>
        ///     Short description of the hotbar action.
        /// </summary>
        [ViewVariables]
        public string Description => _description;

        /// <summary>
        ///     Texture path for UI.
        /// </summary>
        [ViewVariables]
        public string TexturePath => _texturePath;

        public HotbarActionId HotbarActionId => _hotbarActionId;

        public void LoadFrom(YamlMappingNode mapping)
        {
            var serializer = YamlObjectSerializer.NewReader(mapping);

            serializer.DataField(ref _name, "name", string.Empty);
            serializer.DataField(ref _id, "id", string.Empty);
            serializer.DataField(ref _description, "description", string.Empty);
            serializer.DataField(ref _texturePath, "icon", string.Empty);
            serializer.DataField(ref _hotbarActionId, "hotbarActionId", HotbarActionId.None);
        }
    }
}
