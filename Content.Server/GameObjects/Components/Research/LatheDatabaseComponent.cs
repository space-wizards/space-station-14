using System.Collections.Generic;
using Content.Shared.GameObjects.Components.Research;
using Content.Shared.Research;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Research
{
    public class LatheDatabaseComponent : SharedLatheDatabaseComponent
    {
        /// <summary>
        ///     Whether new recipes can be added to this database or not.
        /// </summary>
        public bool Static => _static;
        private bool _static = false;

        public override ComponentState GetComponentState()
        {
            return new LatheDatabaseState(GetRecipeIdList());
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _static, "static", false);
        }

        public override void Clear()
        {
            if (Static) return;
            Dirty();
        }

        public override void AddRecipe(LatheRecipePrototype recipe)
        {
            if (Static) return;
            Dirty();
        }

        public override bool RemoveRecipe(LatheRecipePrototype recipe)
        {
            if (Static || !base.RemoveRecipe(recipe)) return false;
            Dirty();
            return true;
        }

        private List<string> GetRecipeIdList()
        {
            var list = new List<string>();

            foreach (var recipe in this)
            {
                list.Add(recipe.ID);
            }

            return list;
        }
    }
}
