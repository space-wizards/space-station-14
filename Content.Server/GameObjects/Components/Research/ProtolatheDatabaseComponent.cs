using System.Collections.Generic;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Research;
using Content.Shared.Research;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Research
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedLatheDatabaseComponent))]
    public class ProtolatheDatabaseComponent : SharedProtolatheDatabaseComponent
    {
        public override string Name => "ProtolatheDatabase";

        public override ComponentState GetComponentState()
        {
            return new ProtolatheDatabaseState(GetRecipeIdList());
        }

        public void Sync()
        {
            if (!Owner.TryGetComponent(out ResearchClientComponent client)) return;
            if (!client.ConnectedToServer) return;

            foreach (var technology in client.Server.UnlockedTechnologies)
            {
                foreach (var recipe in technology.UnlockedRecipes)
                {
                    UnlockRecipe(recipe);
                }
            }

            Dirty();
        }

        public override void Clear()
        {
            base.Clear();
            Dirty();
        }

        public override void AddRecipe(LatheRecipePrototype recipe)
        {
            base.AddRecipe(recipe);
            Dirty();
        }

        public override bool RemoveRecipe(LatheRecipePrototype recipe)
        {
            if (!base.RemoveRecipe(recipe)) return false;
            Dirty();
            return true;
        }

        public bool UnlockRecipe(LatheRecipePrototype recipe)
        {
            if (!ProtolatheRecipes.Contains(recipe)) return false;

            AddRecipe(recipe);

            return true;
        }

        public bool UnlockRecipe(string id)
        {
            var recipe = (LatheRecipePrototype)IoCManager.Resolve<PrototypeManager>().Index(typeof(LatheRecipePrototype), id);
            return UnlockRecipe(recipe);
        }
    }
}
