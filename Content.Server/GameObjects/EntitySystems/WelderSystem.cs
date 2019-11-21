using System;
using Content.Server.GameObjects.Components.Chemistry;
using Content.Server.GameObjects.Components.Interactable.Tools;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects.EntitySystems
{
    class WelderSystem : EntitySystem
    {   

        public override void SubscribeEvents()
        {
            base.SubscribeEvents();
            SubscribeEvent<UseInHandMessage>(WelderUseCallback);
        }

        private void WelderUseCallback(object sender, UseInHandMessage ev)
        {

            if(ev.Used is IEntity senderEnt && senderEnt.IsValid()
                && senderEnt.TryGetComponent(out WelderComponent welder)
                && !ev.Handled
                )
            {
                ev.Handled = ToggleWelderStatus(welder);
            }

        }


        public override void Initialize()
        {
            base.Initialize();
            EntityQuery = new TypeEntityQuery(typeof(WelderComponent));
        }

        public override void Update(float frameTime)
        {
            foreach (var entity in RelevantEntities)
            {
                var welder = entity.GetComponent<WelderComponent>();
                UpdateWelder(welder);
            }
        }

        private void UpdateWelder(WelderComponent welder)
        {
            if(!welder.Activated)
            {
                return;
            }

            if(welder.Owner.TryGetComponent<SolutionComponent>(out var solution))
            {
                solution.TryRemoveReagent(welder.WelderFuelReagentName, 1);
            }

            if(solution.GetReagentQuantity(welder.WelderFuelReagentName) == 0)
            {
                ToggleWelderStatus(welder);
            }

        }

        private bool ToggleWelderStatus(WelderComponent welder)
        {
            if (welder.Activated)
            {
                welder.Activated = false;
                // Layer 1 is the flame.
                if(welder.Owner.TryGetComponent<SpriteComponent>(out var sprite))
                {
                    sprite.LayerSetVisible(1, false);
                }
                return true;
            }
            else if (CanActivate(welder))
            {
                welder.Activated = true;
                if (welder.Owner.TryGetComponent<SpriteComponent>(out var sprite))
                {
                    sprite.LayerSetVisible(1, true);
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool CanActivate(WelderComponent welder)
        {
            return welder.Owner.TryGetComponent<SolutionComponent>(out var solution)
                && solution.GetReagentQuantity(welder.WelderFuelReagentName) > 0;   
        }

        public bool WelderTryUse(WelderComponent welder, int fuelToUse)
        {
            if(!welder.Activated || WelderHasEnoughFuelToUse(welder, fuelToUse))
            {
                return false;
            }

            return welder.Owner.TryGetComponent<SolutionComponent>(out var solution)
                && solution.TryRemoveReagent(welder.WelderFuelReagentName, fuelToUse);
        }

        public bool WelderHasEnoughFuelToUse(WelderComponent welder, int requiredFuel)
        {
            return welder.Owner.TryGetComponent<SolutionComponent>(out var solution)
                && solution.GetReagentQuantity(welder.WelderFuelReagentName) > requiredFuel;
        }
    }
}
