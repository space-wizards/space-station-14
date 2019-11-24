using System;
using Content.Server.GameObjects.Components;
using Content.Server.GameObjects.Components.Chemistry;
using Content.Server.GameObjects.Components.Explosive;
using Content.Server.GameObjects.Components.Interactable.Tools;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects.EntitySystems
{
    class FuelTankSystem : EntitySystem
    {
        public override void SubscribeEvents()
        {
            base.SubscribeEvents();
            SubscribeEvent<AttackByMessage>(FuelTankAttackBy);
        }

        private void FuelTankAttackBy(object sender, AttackByMessage ev)
        {
            if (ev.ItemInHand is IEntity ent && ent.IsValid()
                && ent.TryGetComponent<WelderComponent>(out var welder)
                && !ev.Handled
                )
            {
                if(welder.Activated
                    && ev.Attacked.TryGetComponent<ExplosiveComponent>(out var explosive)
                    && ev.Attacked.TryGetComponent<SolutionComponent>( out var solution)
                    && solution.GetReagentQuantity(welder.WelderFuelReagentName) != 0
                    )
                {
                    explosive.Explosion();
                }
            }

               
        }
        public override void Initialize()
        {
            base.Initialize();
            EntityQuery = new TypeEntityQuery(typeof(FuelTankComponent));
        }
    }
}
