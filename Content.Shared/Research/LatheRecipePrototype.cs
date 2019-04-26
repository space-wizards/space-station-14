using System;
using System.Collections.Generic;
using Content.Shared.GameObjects.Components.Research;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.Research
{
    [NetSerializable, Serializable, Prototype("latheRecipe")]
    public class LatheRecipePrototype : IPrototype, IIndexedPrototype
    {
        private string _name;
        private string _id;
        private SpriteSpecifier _icon;
        private string _description;
        private string _result;
        private int _completeTime;
        private Dictionary<string, int> _requiredMaterials;

        public string ID => _id;

        /// <summary>
        ///     Name displayed in the lathe GUI.
        /// </summary>
        public string Name
        {
            get
            {
                if (_name.Trim().Length != 0) return _name;
                var protoMan = IoCManager.Resolve<IPrototypeManager>();
                if (protoMan == null) return _description;
                protoMan.TryIndex(_result, out EntityPrototype prototype);
                if (prototype?.Name != null)
                    _name = prototype.Name;
                return _name;
            }
        }

        /// <summary>
        ///     Short description displayed in the lathe GUI.
        /// </summary>
        public string Description
        {
            get
            {
                if (_description.Trim().Length != 0) return _description;
                var protoMan = IoCManager.Resolve<IPrototypeManager>();
                if (protoMan == null) return _description;
                protoMan.TryIndex(_result, out EntityPrototype prototype);
                if (prototype?.Description != null)
                    _description = prototype.Description;
                return _description;
            }
        }

        /// <summary>
        ///     Texture path used in the lathe GUI.
        /// </summary>
        public SpriteSpecifier Icon => _icon;

        /// <summary>
        ///     The prototype name of the resulting entity when the recipe is printed.
        /// </summary>
        public string Result => _result;

        /// <summary>
        ///     The materials required to produce this recipe.
        ///     Takes a material ID as string.
        /// </summary>
        public Dictionary<string, int> RequiredMaterials
        {
            get => _requiredMaterials;
            private set => _requiredMaterials = value;
        }


    /// <summary>
        ///     How many milliseconds it'll take for the lathe to finish this recipe.
        ///     Might lower depending on the lathe's upgrade level.
        /// </summary>
        public int CompleteTime => _completeTime;

        public void LoadFrom(YamlMappingNode mapping)
        {
            var serializer = YamlObjectSerializer.NewReader(mapping);

            serializer.DataField(ref _name, "name", string.Empty);
            serializer.DataField(ref _id, "id", string.Empty);
            serializer.DataField(ref _description, "description", string.Empty);
            serializer.DataField(ref _icon, "icon", SpriteSpecifier.Invalid);
            serializer.DataField(ref _result, "result", null);
            serializer.DataField(ref _completeTime, "completetime", 2500);
            serializer.DataField(ref _requiredMaterials, "materials", new Dictionary<string, int>());
        }
    }
}
