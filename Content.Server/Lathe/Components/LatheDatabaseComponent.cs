using Content.Shared.GameObjects.Components.Research;
using Content.Shared.Research;
using Robust.Shared.GameObjects;
using Robust.Shared.Players;
using Robust.Shared.Serialization.Manager.Attributes;
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
        [DataField("static")]
        public bool Static { get; private set; } = false;

        public override ComponentState GetComponentState(ICommonSession player)
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
