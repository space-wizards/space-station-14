using System.Collections.Generic;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Audio;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Weapon.Melee
{
    [RegisterComponent]
    public class StunbatonComponent : MeleeWeaponComponent, IUse
    {
        [Dependency] private IRobustRandom _robustRandom;
        [Dependency] private IEntitySystemManager _entitySystemManager;

        public override string Name => "Stunbaton";

        private bool _activated = false;

        [ViewVariables(VVAccess.ReadWrite)]
        private float _paralyzeChance = 0.25f;

        [ViewVariables(VVAccess.ReadWrite)]
        private float _paralyzeTime = 10f;

        [ViewVariables(VVAccess.ReadWrite)]
        private float _slowdownTime = 5f;

        [ViewVariables]
        public bool Activated => _activated;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _paralyzeChance, "paralyzeChance", 0.25f);
            serializer.DataField(ref _paralyzeTime, "paralyzeTime", 10f);
            serializer.DataField(ref _slowdownTime, "slowdownTime", 5f);
        }

        public override void OnHitEntities(IEnumerable<IEntity> entities)
        {
            if (!Activated)
                return;

            _entitySystemManager.GetEntitySystem<AudioSystem>()
                .Play("/Audio/weapons/egloves.ogg", Owner, AudioHelpers.WithVariation(0.25f));

            foreach (var entity in entities)
            {
                if (!entity.TryGetComponent(out StunnableComponent stunnable)) continue;

                if(_robustRandom.Prob(_paralyzeChance))
                    stunnable.Paralyze(_paralyzeTime);
                else
                    stunnable.Slowdown(_slowdownTime);
            }
        }

        public bool UseEntity(UseEntityEventArgs eventArgs)
        {
            var sprite = Owner.GetComponent<SpriteComponent>();
            var item = Owner.GetComponent<ItemComponent>();

            if (_activated)
            {
                item.EquippedPrefix = "off";
                sprite.LayerSetState(0, "stunbaton_off");
                _activated = false;
            }
            else
            {
                _entitySystemManager.GetEntitySystem<AudioSystem>()
                    .Play(AudioHelpers.GetRandomFileFromSoundCollection("sparks"), Owner, AudioHelpers.WithVariation(0.25f));

                item.EquippedPrefix = "on";
                sprite.LayerSetState(0, "stunbaton_on");
                _activated = true;
            }

            return true;
        }
    }
}
