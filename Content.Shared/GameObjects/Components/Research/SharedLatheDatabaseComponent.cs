using System;
using System.Collections;
using System.Collections.Generic;
using Content.Shared.Research;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Research
{
    public class SharedLatheDatabaseComponent : Component, IEnumerable<LatheRecipePrototype>
    {
        public override string Name => "LatheDatabase";
        public override uint? NetID => ContentNetIDs.LATHE_DATABASE;

        /// <summary>
        ///     Whether new recipes can be added to this database or not.
        /// </summary>
        public bool Static => _static;
        private bool _static = false;

        private List<LatheRecipePrototype> _recipes = new List<LatheRecipePrototype>();

        /// <summary>
        ///     Removes all recipes from the database if it's not static.
        /// </summary>
        /// <returns>Whether it could clear the database or not.</returns>
        public virtual bool Clear()
        {
            if (Static) return false;

            _recipes.Clear();
            return true;
        }

        /// <summary>
        ///     Adds a recipe to the database if it's not static.
        /// </summary>
        /// <param name="recipe">The recipe to be added.</param>
        /// <returns>Whether it could be added or not</returns>
        public virtual bool AddRecipe(LatheRecipePrototype recipe)
        {
            if (Static) return false;

            _recipes.Add(recipe);
            return true;
        }

        /// <summary>
        ///     Removes a recipe from the database if it's not static.
        /// </summary>
        /// <param name="recipe">The recipe to be removed.</param>
        /// <returns>Whether it could be removed or not</returns>
        public virtual bool RemoveRecipe(LatheRecipePrototype recipe)
        {
            return !Static && _recipes.Remove(recipe);
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

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _static, "static", false);
            var recipes = serializer.ReadDataField("recipes", new List<string>());
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            foreach (var id in recipes)
            {
                if (!prototypeManager.TryIndex(id, out LatheRecipePrototype recipe)) continue;
                _recipes.Add(recipe);
            }
        }

        public IEnumerator<LatheRecipePrototype> GetEnumerator()
        {
            return _recipes.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        [NetSerializable, Serializable]
        public class LatheDatabaseSyncMessage : ComponentMessage
        {
            public readonly List<string> Recipes;
            public LatheDatabaseSyncMessage(List<string> recipes)
            {
                Directed = true;
                Recipes = recipes;
            }
        }

        [NetSerializable, Serializable]
        public class LatheDatabaseRecipeAddMessage : ComponentMessage
        {
            public readonly string Recipe;
            public LatheDatabaseRecipeAddMessage(string recipe)
            {
                Directed = true;
                Recipe = recipe;
            }
        }

        [NetSerializable, Serializable]
        public class LatheDatabaseRecipeRemoveMessage : ComponentMessage
        {
            public readonly string Recipe;
            public LatheDatabaseRecipeRemoveMessage(string recipe)
            {
                Directed = true;
                Recipe = recipe;
            }
        }

        [NetSerializable, Serializable]
        public class LatheDatabaseClearMessage : ComponentMessage
        {
            public LatheDatabaseClearMessage()
            {
                Directed = true;
            }
        }
    }
}
