using Content.Shared.Research.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Lathe
{
    [ComponentReference(typeof(SharedLatheDatabaseComponent))]
    [NetworkedComponent()]
    public abstract class SharedProtolatheDatabaseComponent : SharedLatheDatabaseComponent, ISerializationHooks
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

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
    public sealed class ProtolatheDatabaseState : ComponentState
    {
        public readonly List<string> Recipes;
        public ProtolatheDatabaseState(List<string> recipes)
        {
            Recipes = recipes;
        }
    }
}
