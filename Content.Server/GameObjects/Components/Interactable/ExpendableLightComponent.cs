
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
using Robust.Shared.GameObjects.EntitySystemMessages;
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
        public bool Activated => CurrentState == ExpendableLightState.Lit || CurrentState == ExpendableLightState.Fading;

        [ViewVariables]
        private float _stateExpiryTime = default;
        private AppearanceComponent _appearance = default;

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

            CurrentState = ExpendableLightState.BrandNew;
            Owner.EnsureComponent<PointLightComponent>();
            Owner.TryGetComponent(out _appearance);
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

                CurrentState = ExpendableLightState.Lit;
                _stateExpiryTime = GlowDuration;
                
                UpdateSpriteAndSounds(Activated);
                UpdateVisualizer();

                return true;
            }

            return false;
        }

        private void UpdateVisualizer()
        {
            switch (CurrentState)
            {
                case ExpendableLightState.Lit:
                    _appearance.SetData(ExpendableLightVisuals.State, TurnOnBehaviourID);
                    break;

                case ExpendableLightState.Fading:
                    _appearance.SetData(ExpendableLightVisuals.State, FadeOutBehaviourID);
                    break;

                case ExpendableLightState.Dead:
                    _appearance.SetData(ExpendableLightVisuals.State, string.Empty);
                    break;

                default:
                    break;
            }
        }

        private void UpdateSpriteAndSounds(bool on)
        {
            if (Owner.TryGetComponent(out SpriteComponent sprite))
            {
                switch (CurrentState)
                {
                    case ExpendableLightState.Lit:

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

                    case ExpendableLightState.Fading:
                        break;

                    default:
                    case ExpendableLightState.Dead:

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
                    case ExpendableLightState.Lit:

                        CurrentState = ExpendableLightState.Fading;
                        _stateExpiryTime = FadeOutDuration;

                        UpdateVisualizer();

                        break;

                    default:
                    case ExpendableLightState.Fading:

                        CurrentState = ExpendableLightState.Dead;
                        Owner.Name = SpentName;
                        Owner.Description = SpentDesc;

                        UpdateSpriteAndSounds(Activated);
                        UpdateVisualizer();

                        if (Owner.TryGetComponent<ItemComponent>(out var item))
                        {
                            item.EquippedPrefix = "off";
                        }

                        break;
                }
            }
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

                if (component.CurrentState == ExpendableLightState.BrandNew)
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
