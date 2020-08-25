using System.Collections.Generic;
using Content.Server.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.Timers;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Weapon.Melee
{
    [RegisterComponent]
    public class FlashComponent : MeleeWeaponComponent, IUse, IExamine
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly ISharedNotifyManager _notifyManager = default!;

        public override string Name => "Flash";

        [ViewVariables(VVAccess.ReadWrite)] private int _flashDuration = 5000;
        [ViewVariables(VVAccess.ReadWrite)] private int _uses = 5;
        [ViewVariables(VVAccess.ReadWrite)] private float _range = 3f;
        [ViewVariables(VVAccess.ReadWrite)] private int _aoeFlashDuration = 5000 / 3;
        [ViewVariables(VVAccess.ReadWrite)] private float _slowTo = 0.75f;
        private bool _flashing;

        private int Uses
        {
            get => _uses;
            set
            {
                _uses = value;
                Dirty();
            }
        }

        private bool HasUses => _uses > 0;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _flashDuration, "duration", 5000);
            serializer.DataField(ref _uses, "uses", 5);
            serializer.DataField(ref _range, "range", 7f);
            serializer.DataField(ref _aoeFlashDuration, "aoeFlashDuration", _flashDuration / 3);
            serializer.DataField(ref _slowTo, "slowTo", 0.75f);
        }

        protected override bool OnHitEntities(IReadOnlyList<IEntity> entities, AttackEventArgs eventArgs)
        {
            if (entities.Count == 0)
            {
                return false;
            }

            if (!Use(eventArgs.User))
            {
                return false;
            }

            foreach (var entity in entities)
            {
                Flash(entity, eventArgs.User);
            }

            return true;
        }

        public bool UseEntity(UseEntityEventArgs eventArgs)
        {
            if (!Use(eventArgs.User))
            {
                return false;
            }

            foreach (var entity in _entityManager.GetEntitiesInRange(Owner.Transform.GridPosition, _range))
            {
                Flash(entity, eventArgs.User, _aoeFlashDuration);
            }

            return true;
        }

        private bool Use(IEntity user)
        {
            if (HasUses)
            {
                var sprite = Owner.GetComponent<SpriteComponent>();
                if (--Uses == 0)
                {
                    sprite.LayerSetState(0, "burnt");

                    _notifyManager.PopupMessage(Owner, user, Loc.GetString("The flash burns out!"));
                }
                else if (!_flashing)
                {
                    int animLayer = sprite.AddLayerWithState("flashing");
                    _flashing = true;

                    Timer.Spawn(400, () =>
                    {
                        sprite.RemoveLayer(animLayer);
                        _flashing = false;
                    });
                }

                EntitySystem.Get<AudioSystem>().PlayAtCoords("/Audio/Weapons/flash.ogg", Owner.Transform.GridPosition,
                    AudioParams.Default);

                return true;
            }

            return false;
        }

        private void Flash(IEntity entity, IEntity user)
        {
            Flash(entity, user, _flashDuration);
        }

        // TODO: Check if target can be flashed (e.g. things like sunglasses would block a flash)
        private void Flash(IEntity entity, IEntity user, int flashDuration)
        {
            if (entity.TryGetComponent(out ServerOverlayEffectsComponent overlayEffectsComponent))
            {
                if (!overlayEffectsComponent.TryModifyOverlay(nameof(SharedOverlayID.FlashOverlay),
                    overlay =>
                    {
                        if (overlay.TryGetOverlayParameter<TimedOverlayParameter>(out var timed))
                        {
                            timed.Length += flashDuration;
                        }
                    }))
                {
                    var container = new OverlayContainer(SharedOverlayID.FlashOverlay, new TimedOverlayParameter(flashDuration));
                    overlayEffectsComponent.AddOverlay(container);
                }
            }

            if (entity.TryGetComponent(out StunnableComponent stunnableComponent))
            {
                stunnableComponent.Slowdown(flashDuration / 1000f, _slowTo, _slowTo);
            }

            if (entity != user)
            {
                _notifyManager.PopupMessage(user, entity, Loc.GetString("{0:TheName} blinds you with {1:theName}", user, Owner));
            }
        }

        public void Examine(FormattedMessage message, bool inDetailsRange)
        {
            if (!HasUses)
            {
                message.AddText("It's burnt out.");
                return;
            }

            if (inDetailsRange)
            {
                message.AddMarkup(
                    Loc.GetString(
                        "The flash has [color=green]{0}[/color] {1} remaining.",
                        Uses,
                        Loc.GetPluralString("use", "uses", Uses)
                    )
                );
            }
        }
    }
}
