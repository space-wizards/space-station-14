using Content.Shared.Research.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Lathe
{
    [RegisterComponent, NetworkedComponent]
    [AutoGenerateComponentState]
    public sealed partial class EmagLatheRecipesComponent : Component
    {
        /// <summary>
        /// All of the dynamic recipes that the lathe is capable to get using EMAG
        /// </summary>
        [DataField("emagDynamicRecipes", customTypeSerializer: typeof(PrototypeIdListSerializer<LatheRecipePrototype>))]
        [AutoNetworkedField]
        public List<string> EmagDynamicRecipes = new();

        /// <summary>
        /// All of the static recipes that the lathe is capable to get using EMAG
        /// </summary>
        [DataField("emagStaticRecipes", customTypeSerializer: typeof(PrototypeIdListSerializer<LatheRecipePrototype>))]
        [AutoNetworkedField]
        public List<string> EmagStaticRecipes = new();
    }
}
