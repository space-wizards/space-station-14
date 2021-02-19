using System;
using System.Collections.Generic;
using Content.Shared.Research;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.GameObjects.Components.Research
{
    [ComponentReference(typeof(SharedLatheDatabaseComponent))]
    public class SharedProtolatheDatabaseComponent : SharedLatheDatabaseComponent, ISerializationHooks
    {
        public override string Name => "ProtolatheDatabase";

        public sealed override uint? NetID => ContentNetIDs.PROTOLATHE_DATABASE;

        [DataField("protolatherecipes")] private List<string> _recipeIds = new();

        /// <summary>
        ///    A full list of recipes this protolathe can print.
        /// </summary>
        public List<LatheRecipePrototype> ProtolatheRecipes { get; } = new();

        void ISerializationHooks.BeforeSerialization()
        {
            var list = new List<string>();

            foreach (var recipe in ProtolatheRecipes)
            {
                list.Add(recipe.ID);
            }

            _recipeIds = list;
        }

        void ISerializationHooks.AfterDeserialization()
        {
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

            foreach (var id in _recipeIds)
            {
                if (prototypeManager.TryIndex(id, out LatheRecipePrototype recipe))
                {
                    ProtolatheRecipes.Add(recipe);
                }
            }
        }
    }

    [NetSerializable, Serializable]
    public class ProtolatheDatabaseState : ComponentState
    {
        public readonly List<string> Recipes;
        public ProtolatheDatabaseState(List<string> recipes) : base(ContentNetIDs.PROTOLATHE_DATABASE)
        {
            Recipes = recipes;
        }
    }
}
