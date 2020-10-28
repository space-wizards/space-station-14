using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.EntitySystemMessages;
using Robust.Shared.GameObjects.Systems;

namespace Content.Shared.GameObjects.EntitySystems
{

    public sealed class WeightlessStatusSystem : EntitySystem
    {

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<WeightlessChangeMessage>(HandleWeightlessChanged);
        }

        private void HandleWeightlessChanged(WeightlessChangeMessage msg)
        {
            var ent = msg.Entity;
            if (!ent.TryGetComponent(out SharedStatusEffectsComponent status))
            {
                return;
            }

            if(msg.Weightless)
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
