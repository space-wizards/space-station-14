using Content.Shared.Lathe;
using Content.Shared.Research.Prototypes;

namespace Content.Server.Lathe.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedLatheDatabaseComponent))]
    public sealed class LatheDatabaseComponent : SharedLatheDatabaseComponent
    {
        /// <summary>
        ///     Whether new recipes can be added to this database or not.
        /// </summary>
        [ViewVariables]
        [DataField("static")]
        public bool Static { get; private set; } = false;

        public override ComponentState GetComponentState()
        {
            return new LatheDatabaseState(GetRecipeIdList());
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
