using Content.Client.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.Components.Mobs.State;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Client.GameObjects.Components.Mobs.State
{
    public class CriticalState : SharedCriticalState
    {
        public override void EnterState(IEntity entity)
        {
            if (entity.TryGetComponent(out AppearanceComponent appearance))
            {
                appearance.SetData(DamageStateVisuals.State, DamageState.Critical);
            }

            EntitySystem.Get<StandingStateSystem>().Down(entity);
        }

        public override void ExitState(IEntity entity)
        {
            EntitySystem.Get<StandingStateSystem>().Standing(entity);
        }

        public override void UpdateState(IEntity entity) { }
    }
}
