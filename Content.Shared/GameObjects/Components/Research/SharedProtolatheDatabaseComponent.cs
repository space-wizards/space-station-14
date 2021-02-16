using System;
using System.Collections.Generic;
using Content.Shared.Research;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Prototypes.DataClasses.Attributes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.GameObjects.Components.Research
{

    [ComponentReference(typeof(SharedLatheDatabaseComponent))]
    [DataClass(typeof(SharedProtoLatheDatabaseComponentData))]
    public class SharedProtolatheDatabaseComponent : SharedLatheDatabaseComponent
    {
        public override string Name => "ProtolatheDatabase";
        public sealed override uint? NetID => ContentNetIDs.PROTOLATHE_DATABASE;

        [DataClassTarget("protolatherecipes")]
        private readonly List<LatheRecipePrototype> _protolatheRecipes = new();

        /// <summary>
        ///    A full list of recipes this protolathe can print.
        /// </summary>
        public List<LatheRecipePrototype> ProtolatheRecipes => _protolatheRecipes;


        /// <summary>
        ///     Returns a list of the allowed protolathe recipe IDs.
        /// </summary>
        /// <returns>A list of recipe IDs allowed</returns>
        public List<string> GetProtolatheRecipeIdList()
        {
            var list = new List<string>();

            foreach (var recipe in ProtolatheRecipes)
            {
                list.Add(recipe.ID);
            }

            return list;
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
