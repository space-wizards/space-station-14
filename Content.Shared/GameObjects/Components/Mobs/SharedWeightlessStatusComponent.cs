using Content.Shared.GameObjects.Components.Movement;
using Robust.Shared.GameObjects;

namespace Content.Shared.GameObjects.Components.Mobs
{
    public abstract class SharedWeightlessStatusComponent : Component
    {
        //Could probably be better.
        public override string Name => "WeightlessStatus";



        protected void UpdateStatus(bool isWeightless)
        {

            if (!Owner.TryGetComponent(out SharedStatusEffectsComponent status))
            {
                return;
            }

            if(isWeightless)
            {
                status.ChangeStatusEffect(StatusEffect.Weightless,"/Textures/Interface/StatusEffects/Weightless/weightless.png",null);
            }
            else
            {
                status.RemoveStatusEffect(StatusEffect.Weightless);
            }

        }

    }

}
