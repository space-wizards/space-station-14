using System;
using System.Collections.Generic;
using Content.Shared.Research;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Research
{

    [ComponentReference(typeof(SharedLatheDatabaseComponent))]
    public class SharedProtolatheDatabaseComponent : SharedLatheDatabaseComponent
    {
        public override string Name => "ProtolatheDatabase";
        public sealed override uint? NetID => ContentNetIDs.PROTOLATHE_DATABASE;

        private readonly List<LatheRecipePrototype> _protolatheRecipes = new();

        /// <summary>
        ///    A full list of recipes this protolathe can print.
        /// </summary>
        public List<LatheRecipePrototype> ProtolatheRecipes => _protolatheRecipes;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataReadWriteFunction(
                "protolatherecipes",
                new List<string>(),
                recipes =>
                {
                    var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

                    foreach (var id in recipes)
                    {
                        if (prototypeManager.TryIndex(id, out LatheRecipePrototype recipe))
                        {
                            _protolatheRecipes.Add(recipe);
                        }
                    }
                },
                GetProtolatheRecipeIdList);
        }

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
