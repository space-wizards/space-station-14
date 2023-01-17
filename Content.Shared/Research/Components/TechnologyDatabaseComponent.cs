using Content.Shared.Research.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Research.Components
{
    [RegisterComponent, NetworkedComponent]
    public sealed class TechnologyDatabaseComponent : Component
    {
        /// <summary>
        /// The ids of all the technologies which have been unlocked.
        /// </summary>
        [DataField("technologyIds", customTypeSerializer: typeof(PrototypeIdListSerializer<TechnologyPrototype>))]
        public List<string> TechnologyIds = new();

        /// <summary>
        /// The ids of all the lathe recipes which have been unlocked.
        /// This is maintained alongside the TechnologyIds
        /// </summary>
        [DataField("recipeIds", customTypeSerializer: typeof(PrototypeIdListSerializer<LatheRecipePrototype>))]
        public List<string> RecipeIds = new();
    }

    /// <summary>
    /// Event raised on the database whenever its
    /// technologies or recipes are modified.
    /// </summary>
    /// <remarks>
    /// This event is forwarded from the
    /// server to all of it's clients.
    /// </remarks>
    [ByRefEvent]
    public readonly record struct TechnologyDatabaseModifiedEvent;

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
