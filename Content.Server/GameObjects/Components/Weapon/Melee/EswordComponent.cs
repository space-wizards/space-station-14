using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Audio;
using Content.Shared.Interfaces;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Weapon.Melee
{
    [RegisterComponent]
    public class EswordComponent : MeleeWeaponComponent, IUse
    {
#pragma warning disable 649
        [Dependency] private IEntitySystemManager _entitySystemManager;
        [Dependency] private readonly ISharedNotifyManager _notifyManager;
        [Dependency] private readonly ILocalizationManager _localizationManager;
#pragma warning restore 649

        public override string Name => "Esword";

        private bool _activated = false;

        [ViewVariables]
        public bool Activated => _activated;
        private PointLightComponent _pointLight;
        private ClothingComponent _clothingComponent;
        private SpriteComponent _spriteComponent;

        public override void Initialize()
        {
            base.Initialize();

            _pointLight = Owner.GetComponent<PointLightComponent>();
            _spriteComponent = Owner.GetComponent<SpriteComponent>();
            Owner.TryGetComponent(out _clothingComponent);
        }

        private bool ToggleStatus(IEntity user)
        {
            if (Activated)
            {
                TurnOff();
            }
            else
            {
                TurnOn(user);
            }

            return true;
        }

        private void TurnOff()
        {
            var sprite = Owner.GetComponent<SpriteComponent>();
            var item = Owner.GetComponent<ItemComponent>();

            if (!_activated)
            {
                return;
            }

            _entitySystemManager.GetEntitySystem<AudioSystem>().Play("/Audio/weapons/saberoff.ogg", Owner.Transform.GridPosition, AudioHelpers.WithVariation(0.25f));
            item.EquippedPrefix = "off";
            sprite.LayerSetState(0, "esword_off");
            _activated = false;
            SetState(false);

        }

        private void TurnOn(IEntity user)
        {
            if (_activated)
            {
                return;
            }

            var sprite = Owner.GetComponent<SpriteComponent>();
            var item = Owner.GetComponent<ItemComponent>();

            _entitySystemManager.GetEntitySystem<AudioSystem>().Play("/Audio/weapons/saberon.ogg", Owner.Transform.GridPosition, AudioHelpers.WithVariation(0.25f));
            item.EquippedPrefix = "on";
            sprite.LayerSetState(0, "esword_on");
            _activated = true;
            SetState(true);
        }

        private void SetState(bool on)
        {
            _pointLight.Enabled = on;
            if (_clothingComponent != null)
            {
                _clothingComponent.ClothingEquippedPrefix = on ? "On" : "Off";
            }
        }

        public bool UseEntity(UseEntityEventArgs eventArgs)
        {
            ToggleStatus(eventArgs.User);

            return true;
        }

        public void Examine(FormattedMessage message)
        {
            var loc = IoCManager.Resolve<ILocalizationManager>();

            if (Activated)
            {
                message.AddMarkup(loc.GetString("The light is currently [color=darkgreen]on[/color]."));
            }
        }
    }
}
