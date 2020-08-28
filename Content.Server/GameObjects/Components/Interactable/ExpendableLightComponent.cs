
using Content.Server.GameObjects.Components.Items.Clothing;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.Sound;
using Content.Shared.GameObjects.Components;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Verbs;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.ViewVariables;
using Robust.Shared.Audio;

namespace Content.Server.GameObjects.Components.Interactable
{
    /// <summary>
    ///     Component that represents a handheld expendable light which can be activated and eventually dies over time.
    /// </summary>
    [RegisterComponent]
    public class ExpendableLightComponent : SharedExpendableLightComponent, IUse
    {
        private static readonly AudioParams LoopedSoundParams = new AudioParams(0, 1, "Master", 62.5f, 1, AudioMixTarget.Stereo, true, 0.3f);

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
            return TryActivate();
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
        private bool TryActivate()
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

                return true;
            }

            return false;
        }

        private void UpdateVisuals(bool on)
        {
            if (Owner.TryGetComponent(out SpriteComponent sprite))
            {
                switch (CurrentState)
                {
                    case LightState.Lit:

                        if (LoopedSound != string.Empty && Owner.TryGetComponent<LoopingLoopingSoundComponent>(out var loopingSound))
                        {
                            loopingSound.Play(LoopedSound, LoopedSoundParams);
                        }

                        if (LitSound != string.Empty)
                        {
                            EntitySystem.Get<AudioSystem>().PlayFromEntity(LitSound, Owner);
                        }

                        sprite.LayerSetVisible(1, true);
                        sprite.LayerSetState(2, IconStateLit);
                        sprite.LayerSetShader(2, "unshaded");
                        break;

                    case LightState.Fading:
                        break;

                    default:
                    case LightState.Dead:

                        if (DieSound != string.Empty)
                        {
                            EntitySystem.Get<AudioSystem>().PlayFromEntity(DieSound, Owner);
                        }

                        if (LoopedSound != string.Empty && Owner.TryGetComponent<LoopingLoopingSoundComponent>(out var loopSound))
                        {
                            loopSound.StopAllSounds();
                        }

                        sprite.LayerSetVisible(1, false);
                        sprite.LayerSetState(2, IconStateSpent);
                        sprite.LayerSetShader(2, "shaded");
                        break;
                }
            }

            if (Owner.TryGetComponent(out PointLightComponent light))
            {
                _light = light;
                _light.Enabled = on;
            }

            if (Owner.TryGetComponent(out ClothingComponent clothing))
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
            return new ExpendableLightComponentState(CurrentState);
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
                component.TryActivate();
            }
        }
    }
}
