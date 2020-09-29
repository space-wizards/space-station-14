using Content.Shared.GameObjects.Components.Research;
using Content.Shared.Research;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Research
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedLatheDatabaseComponent))]
    public class LatheDatabaseComponent : SharedLatheDatabaseComponent
    {
        /// <summary>
        ///     Whether new recipes can be added to this database or not.
        /// </summary>
        [ViewVariables]
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
            base.Clear();
            Dirty();
        }

        public override void AddRecipe(LatheRecipePrototype recipe)
        {
            if (Static) return;
            base.AddRecipe(recipe);
            Dirty();
        }

        public override bool RemoveRecipe(LatheRecipePrototype recipe)
        {
            if (Static || !base.RemoveRecipe(recipe)) return false;
            Dirty();
            return true;
        }
    }
}
