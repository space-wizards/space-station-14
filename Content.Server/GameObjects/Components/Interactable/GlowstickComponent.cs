
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
    ///     Component that represents a handheld glowstick which can be activated and eventually dies over time.
    /// </summary>
    [RegisterComponent]
    internal sealed class GlowstickComponent : SharedGlowstickComponent, IUse
    { 
        [Dependency] private readonly ISharedNotifyManager _notifyManager = default!;

        /// <summary>
        ///     Status of light, whether or not it is emitting light.
        /// </summary>
        [ViewVariables]
        public bool Activated => CurrentState == GlowstickState.Lit || CurrentState == GlowstickState.Fading;

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

            CurrentState = GlowstickState.BrandNew;
            Owner.EnsureComponent<PointLightComponent>();
            Dirty();
        }

        /// <summary>
        ///     Enables the light if it is not active. Once active it cannot be turned off.
        /// </summary>
        /// <returns>True if the light's status was toggled, false otherwise.</returns>
        private bool TryActivate(IEntity user)
        {
            if (!Activated)
            {
                if (Owner.TryGetComponent<ItemComponent>(out var item))
                {
                    item.EquippedPrefix = "on";
                }

                CurrentState = GlowstickState.Lit;
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
                    case GlowstickState.Lit:
                    case GlowstickState.Fading:

                        sprite.LayerSetState(1, IconStateLit);
                        break;

                    default:
                    case GlowstickState.Dead:

                        sprite.LayerSetState(1, IconStateSpent);
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
                    case GlowstickState.Lit:

                        CurrentState = GlowstickState.Fading;
                        _stateExpiryTime = FadeOutDuration;

                        Dirty();

                        break;

                    default:
                    case GlowstickState.Fading:

                        CurrentState = GlowstickState.Dead;
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
            return new GlowstickComponentState(CurrentState, _stateExpiryTime);
        }

        [Verb]
        public sealed class ActivateVerb : Verb<GlowstickComponent>
        {
            protected override void GetData(IEntity user, GlowstickComponent component, VerbData data)
            {
                if (!ActionBlockerSystem.CanInteract(user))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                if (component.CurrentState == GlowstickState.BrandNew)
                {
                    data.Text = "Activate";
                    data.Visibility = VerbVisibility.Visible;
                }
                else
                {
                    data.Visibility = VerbVisibility.Invisible;
                }
            }

            protected override void Activate(IEntity user, GlowstickComponent component)
            {
                component.TryActivate(user);
            }
        }
    }
}
