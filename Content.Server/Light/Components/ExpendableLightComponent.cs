using Content.Server.Clothing.Components;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Light.Component;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Player;
using Robust.Shared.ViewVariables;

namespace Content.Server.Light.Components
{
    /// <summary>
    ///     Component that represents a handheld expendable light which can be activated and eventually dies over time.
    /// </summary>
    [RegisterComponent]
    public class ExpendableLightComponent : SharedExpendableLightComponent, IUse
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        /// <summary>
        ///     Status of light, whether or not it is emitting light.
        /// </summary>
        [ViewVariables]
        public bool Activated => CurrentState == ExpendableLightState.Lit || CurrentState == ExpendableLightState.Fading;

        [ViewVariables]
        private float _stateExpiryTime = default;
        private AppearanceComponent? _appearance = default;

        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
        {
            return TryActivate();
        }

        protected override void Initialize()
        {
            base.Initialize();

            if (_entMan.TryGetComponent<SharedItemComponent?>(Owner, out var item))
            {
                item.EquippedPrefix = "unlit";
            }

            CurrentState = ExpendableLightState.BrandNew;
            Owner.EnsureComponent<PointLightComponent>();
            _entMan.TryGetComponent(Owner, out _appearance);
        }

        /// <summary>
        ///     Enables the light if it is not active. Once active it cannot be turned off.
        /// </summary>
        public bool TryActivate()
        {
            if (!Activated && CurrentState == ExpendableLightState.BrandNew)
            {
                if (_entMan.TryGetComponent<SharedItemComponent?>(Owner, out var item))
                {
                    item.EquippedPrefix = "lit";
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
            _appearance?.SetData(ExpendableLightVisuals.State, CurrentState);

            switch (CurrentState)
            {
                case ExpendableLightState.Lit:
                    _appearance?.SetData(ExpendableLightVisuals.Behavior, TurnOnBehaviourID);
                    break;

                case ExpendableLightState.Fading:
                    _appearance?.SetData(ExpendableLightVisuals.Behavior, FadeOutBehaviourID);
                    break;

                case ExpendableLightState.Dead:
                    _appearance?.SetData(ExpendableLightVisuals.Behavior, string.Empty);
                    break;
            }
        }

        private void UpdateSpriteAndSounds(bool on)
        {
            if (_entMan.TryGetComponent(Owner, out SpriteComponent? sprite))
            {
                switch (CurrentState)
                {
                    case ExpendableLightState.Lit:
                    {
                        SoundSystem.Play(Filter.Pvs(Owner), LitSound.GetSound(), Owner);

                        if (IconStateLit != string.Empty)
                        {
                            sprite.LayerSetState(2, IconStateLit);
                            sprite.LayerSetShader(2, "shaded");
                        }

                        sprite.LayerSetVisible(1, true);
                        break;
                    }
                    case ExpendableLightState.Fading:
                    {
                        break;
                    }
                    default:
                    case ExpendableLightState.Dead:
                    {
                        if (DieSound != null) SoundSystem.Play(Filter.Pvs(Owner), DieSound.GetSound(), Owner);

                        sprite.LayerSetState(0, IconStateSpent);
                        sprite.LayerSetShader(0, "shaded");
                        sprite.LayerSetVisible(1, false);
                        break;
                    }
                }
            }

            if (_entMan.TryGetComponent(Owner, out ClothingComponent? clothing))
            {
                clothing.EquippedPrefix = on ? "Activated" : string.Empty;
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
                        _entMan.GetComponent<MetaDataComponent>(Owner).EntityName = SpentName;
                        _entMan.GetComponent<MetaDataComponent>(Owner).EntityDescription = SpentDesc;

                        UpdateSpriteAndSounds(Activated);
                        UpdateVisualizer();

                        if (_entMan.TryGetComponent<SharedItemComponent?>(Owner, out var item))
                        {
                            item.EquippedPrefix = "unlit";
                        }

                        break;
                }
            }
        }
    }
}
