using System.Collections.Generic;
using Content.Server.GameObjects.Components.Mobs;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Weapon.Melee
{
    [RegisterComponent]
    public class StunbatonComponent : MeleeWeaponComponent
    {
        public override string Name => "Stunbaton";

        private float _paralyzeTime = 10f;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _paralyzeTime, "paralyzeTime", 10f);
        }

        public override void OnHitEntities(IEnumerable<IEntity> entities)
        {
            foreach (var entity in entities)
            {
                if(entity.TryGetComponent(out StunnableComponent stunnable))
                    stunnable.Paralyze(_paralyzeTime);
            }
        }
    }
}
