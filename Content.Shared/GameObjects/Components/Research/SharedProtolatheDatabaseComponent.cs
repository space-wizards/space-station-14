#nullable enable
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
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public override string Name => "ProtolatheDatabase";

        public sealed override uint? NetID => ContentNetIDs.PROTOLATHE_DATABASE;

        [DataField("protolatherecipes")] private List<string> _recipeIds = new();

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
        public ProtolatheDatabaseState(List<string> recipes) : base(ContentNetIDs.PROTOLATHE_DATABASE)
        {
            Recipes = recipes;
        }
    }
}
