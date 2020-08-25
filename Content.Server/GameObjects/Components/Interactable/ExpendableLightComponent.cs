
using Content.Server.GameObjects.Components.Items.Clothing;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Shared.GameObjects.Components;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Verbs;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.ViewVariables;
using System;

namespace Content.Server.GameObjects.Components.Interactable
{
    /// <summary>
    ///     Component that represents a handheld light which can be activated and eventually dies over time.
    /// </summary>
    [RegisterComponent]
    internal sealed class ExpendableLightComponent : SharedExpendableLightComponent, IUse
    { 
        /// <summary>
        ///     Status of light, whether or not it is emitting light.
        /// </summary>
        [ViewVariables]
        public bool Activated => CurrentState == LightState.Lit || CurrentState == LightState.Fading;

        [ViewVariables]
        private float _stateExpiryTime = default;
        private PointLightComponent _light = default;

        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
        {
            return TryActivate(eventArgs.User);
        }

        public override void Initialize()
        {
            base.Initialize();

            if (Owner.TryGetComponent<ItemComponent>(out var item))
            {
                item.EquippedPrefix = "off";
            }

            CurrentState = LightState.BrandNew;
            Owner.EnsureComponent<PointLightComponent>();
            Dirty();
        }

        /// <summary>
        ///     Enables the light if it is not active. Once active it cannot be turned off.
        /// </summary>
        private bool TryActivate(IEntity user)
        {
            if (!Activated)
            {
                if (Owner.TryGetComponent<ItemComponent>(out var item))
                {
                    item.EquippedPrefix = "on";
                }

                CurrentState = LightState.Lit;
                _stateExpiryTime = GlowDuration;

                UpdateVisuals(Activated);
                Dirty();

                EntitySystem.Get<AudioSystem>().PlayFromEntity("/Audio/Items/flashlight_toggle.ogg", Owner);

                return true;
            }

            return false;
        }

        private void UpdateVisuals(bool on)
        {
            if (Owner.TryGetComponent(out SpriteComponent? sprite))
            {
                switch (CurrentState)
                {
                    case LightState.Lit:
                    case LightState.Fading:

                        sprite.LayerSetState(1, IconStateLit);
                        sprite.LayerSetShader(1, "unshaded");
                        break;

                    default:
                    case LightState.Dead:

                        sprite.LayerSetState(1, IconStateSpent);
                        sprite.LayerSetShader(1, "shaded");
                        break;
                }
            }

            if (Owner.TryGetComponent(out PointLightComponent? light))
            {
                _light = light;
                _light.Enabled = on;
            }

            if (Owner.TryGetComponent(out ClothingComponent? clothing))
            {
                clothing.ClothingEquippedPrefix = on ? "Activated" : string.Empty;
            }
        }

        public void Update(float frameTime)
        {
            if (!Activated) return;

            _stateExpiryTime -= frameTime;

            if (_stateExpiryTime <= 0f)
            {
                switch (CurrentState)
                {
                    case LightState.Lit:

                        CurrentState = LightState.Fading;
                        _stateExpiryTime = FadeOutDuration;

                        Dirty();

                        break;

                    default:
                    case LightState.Fading:

                        CurrentState = LightState.Dead;
                        Owner.Name = SpentName;
                        Owner.Description = SpentDesc;

                        UpdateVisuals(Activated);
                        Dirty();

                        if (Owner.TryGetComponent<ItemComponent>(out var item))
                        {
                            item.EquippedPrefix = "off";
                        }

                        break;
                }
            }
        }

        public override ComponentState GetComponentState()
        {
            return new ExpendableLightComponentState(CurrentState, _stateExpiryTime);
        }

        [Verb]
        public sealed class ActivateVerb : Verb<ExpendableLightComponent>
        {
            protected override void GetData(IEntity user, ExpendableLightComponent component, VerbData data)
            {
                if (!ActionBlockerSystem.CanInteract(user))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                if (component.CurrentState == LightState.BrandNew)
                {
                    data.Text = "Activate";
                    data.Visibility = VerbVisibility.Visible;
                }
                else
                {
                    data.Visibility = VerbVisibility.Invisible;
                }
            }

            protected override void Activate(IEntity user, ExpendableLightComponent component)
            {
                component.TryActivate(user);
            }
        }
    }
}
