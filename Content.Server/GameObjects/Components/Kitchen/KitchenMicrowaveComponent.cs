using System.Collections.Generic;
using Content.Server.GameObjects.Components.Sound;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.GameObjects.Components;
using Content.Shared.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.ViewVariables;
using Content.Server.GameObjects.Components.Chemistry;
using Content.Shared.Chemistry;
using Robust.Shared.Serialization;
using Robust.Shared.Interfaces.GameObjects;
using Content.Shared.Prototypes.Kitchen;
using Content.Shared.Kitchen;

namespace Content.Server.GameObjects.Components.Kitchen
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class KitchenMicrowaveComponent : Component, IActivate
    {

#pragma warning disable 649
        [Dependency] private readonly IEntityManager _entityManager;
        [Dependency] private readonly RecipeManager _recipeManager;
#pragma warning restore 649

        public override string Name => "Microwave";

        [ViewVariables]
        private SolutionComponent _contents;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

        }

        public override void Initialize()
        {
            base.Initialize();
            if (_contents == null)
            {
                if (Owner.TryGetComponent(out SolutionComponent solutionComponent))
                {
                    _contents = solutionComponent;
                }
                else
                {
                    _contents = Owner.AddComponent<SolutionComponent>();
                }
            }
        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            if(_contents.ReagentList.Count > 0)
            {
                foreach(var r in _recipeManager.Recipes)
                {
                    if(CanSatisfyRecipe(r))
                    {
                        var resultPrototype = r.Result;
                        _entityManager.SpawnEntity(resultPrototype, Owner.Transform.GridPosition);
                        return;
                    }

                }
            }

        }

        private bool CanSatisfyRecipe(MealRecipePrototype recipe)
        {
            foreach(var ingredient in recipe.Ingredients)
            {
                var ingName = ingredient.Key.ToString();
                var ingQuantity = ingredient.Value;
                if (_contents.ContainsReagent(ingName, out var amt) && amt >= ingQuantity)
                {
                    _contents.TryRemoveReagent(ingName, ReagentUnit.New(ingQuantity));
                    return true;
                }

            }
            return false;

        }



    }
}
