using Content.Shared.Research.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Research.Components
{
    [RegisterComponent, NetworkedComponent]
    public sealed class TechnologyDatabaseComponent : Component
    {
        [DataField("technologyIds", customTypeSerializer: typeof(PrototypeIdListSerializer<TechnologyPrototype>))]
        public List<string> TechnologyIds = new();

        [DataField("recipeIds", customTypeSerializer: typeof(PrototypeIdListSerializer<LatheRecipePrototype>))]
        public List<string> RecipeIds = new();
    }

    [Serializable, NetSerializable]
    public sealed class TechnologyDatabaseState : ComponentState
    {
        public List<string> Technologies;
        public List<string> Recipes;

        public TechnologyDatabaseState(List<string> technologies, List<string> recipes)
        {
            Technologies = technologies;
            Recipes = recipes;
        }
    }
}
