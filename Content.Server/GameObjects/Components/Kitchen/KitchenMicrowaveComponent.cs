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
using Content.Shared.Kitchen;

namespace Content.Server.GameObjects.Components.Kitchen
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class KitchenMicrowaveComponent : Component, IActivate
    {

#pragma warning disable 649
        [Dependency] private readonly IPrototypeManager _prototypeManager;
        [Dependency] private readonly IEntityManager _entityManager;
#pragma warning restore 649

        public override string Name => "Microwave";

        private AppearanceComponent _appearanceComponent;

        [ViewVariables]
        private string _useSound;
        [ViewVariables]
        private string _outputPrototype;
        [ViewVariables]
        private SolutionComponent _contents;

        private static List<MicrowaveMealRecipePrototype> _allRecipes;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            if(_allRecipes == null)
            {
                _allRecipes = new List<MicrowaveMealRecipePrototype>();
                foreach (var recipe in _prototypeManager.EnumeratePrototypes<MicrowaveMealRecipePrototype>())
                {

                    _allRecipes.Add(recipe);

                }
                _allRecipes.Sort(new RecipeComparer());
            }

        }

        private class RecipeComparer : IComparer<MicrowaveMealRecipePrototype>
        {
            int IComparer<MicrowaveMealRecipePrototype>.Compare(MicrowaveMealRecipePrototype x, MicrowaveMealRecipePrototype y)
            {
                if(x == null || y == null)
                {
                    return 0;
                }

                if (x.Ingredients.Count < y.Ingredients.Count)
                {
                    return 1;
                }

                if (x.Ingredients.Count > y.Ingredients.Count)
                {
                    return -1;
                }

                return 0;
            }
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
                foreach (var r in _allRecipes)
                {
                    if (CanSatisfyRecipe(r))
                    {
                        var outputFromRecipe = r.OutPutPrototype;
                        _entityManager.SpawnEntity(outputFromRecipe, Owner.Transform.GridPosition);
                        return;
                    }

                }
            }

        }

        private bool CanSatisfyRecipe(MicrowaveMealRecipePrototype recipe)
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
