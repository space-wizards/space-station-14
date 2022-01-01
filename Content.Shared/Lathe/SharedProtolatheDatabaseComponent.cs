using System;
using System.Collections.Generic;
using Content.Shared.Research.Prototypes;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Lathe
{
    [ComponentReference(typeof(SharedLatheDatabaseComponent))]
    [NetworkedComponent()]
    public class SharedProtolatheDatabaseComponent : SharedLatheDatabaseComponent, ISerializationHooks
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public override string Name => "ProtolatheDatabase";

        [DataField("protolatherecipes", customTypeSerializer:typeof(PrototypeIdListSerializer<LatheRecipePrototype>))]
        private List<string> _recipeIds = new();

        /// <summary>
        ///    A full list of recipes this protolathe can print.
        /// </summary>
        public IEnumerable<LatheRecipePrototype> ProtolatheRecipes
        {
            get
            {
                foreach (var id in _recipeIds)
                {
                    yield return _prototypeManager.Index<LatheRecipePrototype>(id);
                }
            }
        }
    }

    [NetSerializable, Serializable]
    public class ProtolatheDatabaseState : ComponentState
    {
        public readonly List<string> Recipes;
        public ProtolatheDatabaseState(List<string> recipes)
        {
            Recipes = recipes;
        }
    }
}
