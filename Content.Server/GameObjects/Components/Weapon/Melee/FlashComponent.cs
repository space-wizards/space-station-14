using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Mobs;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Timer = Robust.Shared.Timers.Timer;

namespace Content.Server.GameObjects.Components.Weapon.Melee
{
    [RegisterComponent]
    public class FlashComponent : MeleeWeaponComponent, IUse
    {
        public override string Name => "Flash";
        private int _lastsForMs = 5000;
        private float _fadeFalloffExp = 8f;
        private float _colorStrength = 0f;
        private float _flickerStrength = 0.3f;
        private int _flickerRate = 30;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _lastsForMs, "lasts_for", 5000);
            serializer.DataField(ref _fadeFalloffExp, "fade_falloff_exp", 8f);
            serializer.DataField(ref _colorStrength, "color_strength", 0f);
            serializer.DataField(ref _flickerStrength, "flicker_strength", 0.3f);
            serializer.DataField(ref _flickerRate, "flicker_rate", 30);
        }

        public override bool OnHitEntities(IReadOnlyList<IEntity> entities)
        {
            foreach (var entity in entities)
            {
                Flash(entity);
            }

            return true;
        }

        public bool UseEntity(UseEntityEventArgs eventArgs)
        {
            Flash(eventArgs.User);

            return true;
        }

        private void Flash(IEntity entity)
        {
            if (entity.TryGetComponent(out ServerOverlayEffectsComponent overlayEffectsComponent))
            {
                overlayEffectsComponent.ChangeOverlay(ScreenEffects.Flash);
                Timer.Spawn(_lastsForMs, () =>
                {
                    overlayEffectsComponent.ChangeOverlay(ScreenEffects.None);
                });
            }
        }
    }
}
