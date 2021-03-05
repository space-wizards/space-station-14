using System.Collections.Generic;
using Content.Server.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Weapon.Melee
{
    [RegisterComponent]
    public class FlashComponent : MeleeWeaponComponent, IUse, IExamine
    {
        public override string Name => "Flash";

        public FlashComponent() { Range = 7f; }

        [DataField("duration")] [ViewVariables(VVAccess.ReadWrite)] private int _flashDuration = 5000;
        [DataField("uses")] [ViewVariables(VVAccess.ReadWrite)] private int _uses = 5;
        [ViewVariables(VVAccess.ReadWrite)] private float _range => Range;
        [ViewVariables(VVAccess.ReadWrite)] private int _aoeFlashDuration => _internalAoeFlashDuration ?? _flashDuration / 3;
        [DataField("aoeFlashDuration")] private int? _internalAoeFlashDuration;
        [DataField("slowTo")] [ViewVariables(VVAccess.ReadWrite)] private float _slowTo = 0.75f;
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

        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
        {
            if (!Use(eventArgs.User))
            {
                return false;
            }

            foreach (var entity in Owner.EntityManager.GetEntitiesInRange(Owner.Transform.Coordinates, _range))
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
                    Owner.PopupMessage(user, Loc.GetString("flash-component-becomes-empty"));
                }
                else if (!_flashing)
                {
                    int animLayer = sprite.AddLayerWithState("flashing");
                    _flashing = true;

                    Owner.SpawnTimer(400, () =>
                    {
                        sprite.RemoveLayer(animLayer);
                        _flashing = false;
                    });
                }

                EntitySystem.Get<AudioSystem>().PlayAtCoords("/Audio/Weapons/flash.ogg", Owner.Transform.Coordinates,
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
        // TODO: Merge with the code in FlashableComponent
        private void Flash(IEntity entity, IEntity user, int flashDuration)
        {
            if (entity.TryGetComponent(out FlashableComponent flashable))
            {
                flashable.Flash(flashDuration / 1000d);
            }

            if (entity.TryGetComponent(out StunnableComponent stunnableComponent))
            {
                stunnableComponent.Slowdown(flashDuration / 1000f, _slowTo, _slowTo);
            }

            if (entity != user)
            {
                user.PopupMessage(entity,
                    Loc.GetString(
                        "flash-component-user-blinds-you",
                        ("user", user)
                    )
                );
            }
        }

        public void Examine(FormattedMessage message, bool inDetailsRange)
        {
            if (!HasUses)
            {
                message.AddText(Loc.GetString("flash-component-examine-empty"));
                return;
            }

            if (inDetailsRange)
            {
                message.AddMarkup(
                    Loc.GetString(
                        "flash-component-examine-detail-count",
                        ("count", Uses),
                        ("markupCountColor", "green")
                    )
                );
            }
        }
    }
}
