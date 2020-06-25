using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.Interfaces;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using Timer = Robust.Shared.Timers.Timer;

namespace Content.Server.GameObjects.Components.Weapon.Melee
{
    [RegisterComponent]
    public class FlashComponent : MeleeWeaponComponent, IUse, IExamine
    {
#pragma warning disable 649
        [Dependency] private readonly ILocalizationManager _localizationManager;
        [Dependency] private readonly ISharedNotifyManager _notifyManager;
#pragma warning restore 649

        public override string Name => "Flash";

        [ViewVariables(VVAccess.ReadWrite)] private int _lastsForMs = 5000;
        [ViewVariables(VVAccess.ReadWrite)] private float _fadeFalloffExp = 8f;
        [ViewVariables(VVAccess.ReadWrite)] private int _usesRemaining = 5;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _lastsForMs, "lasts_for", 5000);
            serializer.DataField(ref _fadeFalloffExp, "fade_falloff_exp", 8f);
            serializer.DataField(ref _usesRemaining, "uses", 5);
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
            if (_usesRemaining <= 0)
            {
                return;
            }

            var sprite = Owner.GetComponent<SpriteComponent>();
            if (--_usesRemaining <= 0)
            {
                sprite.LayerSetState(0, "burnt");
            }

            EntitySystem.Get<AudioSystem>().PlayAtCoords("/Audio/weapons/flash.ogg", Owner.Transform.GridPosition,
                AudioParams.Default);

            if (entity.TryGetComponent(out ServerOverlayEffectsComponent overlayEffectsComponent))
            {
                overlayEffectsComponent.AddOverlays("FlashOverlay");
                Timer.Spawn(_lastsForMs, () =>
                {
                    overlayEffectsComponent.RemoveOverlays("FlashOverlay");
                });
            }

            Dirty();
        }

        public void Examine(FormattedMessage message, bool inDetailsRange)
        {
            if (_usesRemaining <= 0)
            {
                message.AddText("It's burnt out.");
                return;
            }

            if (inDetailsRange)
            {
                message.AddMarkup(_localizationManager.GetString(
                    $"The flash has [color=green]{_usesRemaining}[/color] uses remaining."));
            }
        }
    }
}
