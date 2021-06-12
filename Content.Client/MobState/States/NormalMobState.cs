using Content.Shared.MobState;
using Content.Shared.MobState.State;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Client.MobState.States
{
    public class NormalMobState : SharedNormalMobState
    {
        public override void EnterState(IEntity entity)
        {
            base.EnterState(entity);

            if (entity.TryGetComponent(out AppearanceComponent? appearance))
            {
                appearance.SetData(DamageStateVisuals.State, DamageState.Alive);
            }
        }
    }
}
