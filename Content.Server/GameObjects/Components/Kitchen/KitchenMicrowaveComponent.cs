using Content.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.ViewVariables;
using Content.Server.GameObjects.Components.Chemistry;
using Content.Shared.Chemistry;
using Robust.Shared.Serialization;
using Robust.Shared.Interfaces.GameObjects;
using Content.Shared.Prototypes.Kitchen;
using Content.Shared.Kitchen;
using Robust.Shared.Timers;
using Robust.Server.GameObjects;
using Content.Shared.GameObjects.Components.Power;
using Robust.Server.GameObjects.EntitySystems;
using Content.Server.GameObjects.Components.Power;

namespace Content.Server.GameObjects.Components.Kitchen
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    public class KitchenMicrowaveComponent : Component, IActivate
    {

#pragma warning disable 649
        [Dependency] private readonly IEntitySystemManager _entitySystemManager;
        [Dependency] private readonly IEntityManager _entityManager;
        [Dependency] private readonly RecipeManager _recipeManager;
#pragma warning restore 649

        public override string Name => "Microwave";

        private int _cookTimeSeconds;
        private string _badRecipeName;
        [ViewVariables]
        private SolutionComponent _contents;

        private AppearanceComponent _appearance;

        private AudioSystem _audioSystem;

        private PowerDeviceComponent _powerDevice;
        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _badRecipeName, "failureResult", "FoodBadRecipe");
            serializer.DataField(ref _cookTimeSeconds, "cookTime", 5000);
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

            _appearance = Owner.GetComponent<AppearanceComponent>();
            _powerDevice = Owner.GetComponent<PowerDeviceComponent>();
            _audioSystem = _entitySystemManager.GetEntitySystem<AudioSystem>();
        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            if (_contents.ReagentList.Count == 0 || !_powerDevice.Powered)
            {
                return;
            }
            foreach(var r in _recipeManager.Recipes)
            {
                if(CanSatisfyRecipe(r))
                {
                    SetAppearance(MicrowaveVisualState.Cooking);
                    Timer.Spawn(_cookTimeSeconds, () =>
                    {
                        RemoveContents(r);
                        _entityManager.SpawnEntity(r.Result, Owner.Transform.GridPosition);

                        _audioSystem.Play("/Audio/machines/ding.ogg");
                        SetAppearance(MicrowaveVisualState.Idle);
                    });                  
                    return;
                }
            }

            SetAppearance(MicrowaveVisualState.Cooking);
            Timer.Spawn(_cookTimeSeconds, () =>
            {
                _contents.RemoveAllSolution();
                _entityManager.SpawnEntity(_badRecipeName, Owner.Transform.GridPosition);
                _audioSystem.Play("/Audio/machines/ding.ogg");
                SetAppearance(MicrowaveVisualState.Idle);
            });
        }

        private bool CanSatisfyRecipe(FoodRecipePrototype recipe)
        {
            foreach (var item in recipe.Ingredients)
            {
                if (!_contents.ContainsReagent(item.Key, out var amount))
                {
                    return false;
                }

                if (amount.Int() < item.Value)
                {
                    return false;
                }
            }

            return true;
        }

        private void RemoveContents(FoodRecipePrototype recipe)
        {
            foreach(var item in recipe.Ingredients)
            {
                _contents.TryRemoveReagent(item.Key, ReagentUnit.New(item.Value));
            }
        }

        private void SetAppearance(MicrowaveVisualState state)
        {
            if (_appearance != null || Owner.TryGetComponent(out _appearance))
                _appearance.SetData(PowerDeviceVisuals.VisualState, state);
        }
    }
}
