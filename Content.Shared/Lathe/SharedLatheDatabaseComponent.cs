using System;
using System.Collections;
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
    [NetworkedComponent()]
    public class SharedLatheDatabaseComponent : Component, IEnumerable<LatheRecipePrototype>, ISerializationHooks
    {
        public override string Name => "LatheDatabase";

        [DataField("recipes", customTypeSerializer: typeof(PrototypeIdListSerializer<LatheRecipePrototype>))] private List<string> _recipeIds = new();

        private readonly List<LatheRecipePrototype> _recipes = new();

        void ISerializationHooks.BeforeSerialization()
        {
            var list = new List<string>();

            foreach (var recipe in _recipes)
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
                if (prototypeManager.TryIndex(id, out LatheRecipePrototype? recipe))
                {
                    _recipes.Add(recipe);
                }
            }
        }

        /// <summary>
        ///     Removes all recipes from the database if it's not static.
        /// </summary>
        /// <returns>Whether it could clear the database or not.</returns>
        public virtual void Clear()
        {
            _recipes.Clear();
        }

        /// <summary>
        ///     Adds a recipe to the database if it's not static.
        /// </summary>
        /// <param name="recipe">The recipe to be added.</param>
        /// <returns>Whether it could be added or not</returns>
        public virtual void AddRecipe(LatheRecipePrototype recipe)
        {
            if (!Contains(recipe))
                _recipes.Add(recipe);
        }

        /// <summary>
        ///     Removes a recipe from the database if it's not static.
        /// </summary>
        /// <param name="recipe">The recipe to be removed.</param>
        /// <returns>Whether it could be removed or not</returns>
        public virtual bool RemoveRecipe(LatheRecipePrototype recipe)
        {
            return _recipes.Remove(recipe);
        }

        /// <summary>
        ///     Returns whether the database contains the recipe or not.
        /// </summary>
        /// <param name="recipe">The recipe to check</param>
        /// <returns>Whether the database contained the recipe or not.</returns>
        public virtual bool Contains(LatheRecipePrototype recipe)
        {
            return _recipes.Contains(recipe);
        }

        /// <summary>
        ///     Returns whether the database contains the recipe or not.
        /// </summary>
        /// <param name="id">The recipe id to check</param>
        /// <returns>Whether the database contained the recipe or not.</returns>
        public virtual bool Contains(string id)
        {
            foreach (var recipe in _recipes)
            {
                if (recipe.ID == id) return true;
            }
            return false;
        }

        public List<string> GetRecipeIdList()
        {
            var list = new List<string>();

            foreach (var recipe in this)
            {
                list.Add(recipe.ID);
            }

            return list;
        }

        public IEnumerator<LatheRecipePrototype> GetEnumerator()
        {
            return _recipes.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    [NetSerializable, Serializable]
    public class LatheDatabaseState : ComponentState
    {
        public readonly List<string> Recipes;
        public LatheDatabaseState(List<string> recipes)
        {
            Recipes = recipes;
        }
    }
}
