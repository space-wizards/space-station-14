using Content.Server.Clothing.Components;
using Content.Server.Light.Components;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Light.Component;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Player;

namespace Content.Server.Light.EntitySystems
{
    [UsedImplicitly]
    public sealed class ExpendableLightSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ExpendableLightComponent, ComponentInit>(OnExpLightInit);
            SubscribeLocalEvent<ExpendableLightComponent, UseInHandEvent>(OnExpLightUse);
            SubscribeLocalEvent<ExpendableLightComponent, GetVerbsEvent<ActivationVerb>>(AddIgniteVerb);
        }

        public override void Update(float frameTime)
        {
            foreach (var light in EntityManager.EntityQuery<ExpendableLightComponent>())
            {
                UpdateLight(light, frameTime);
            }
        }

        private void UpdateLight(ExpendableLightComponent component, float frameTime)
        {
            if (!component.Activated) return;

            component.StateExpiryTime -= frameTime;

            if (component.StateExpiryTime <= 0f)
            {
                switch (component.CurrentState)
                {
                    case ExpendableLightState.Lit:
                        component.CurrentState = ExpendableLightState.Fading;
                        component.StateExpiryTime = component.FadeOutDuration;

                        UpdateVisualizer(component);

                        break;

                    default:
                    case ExpendableLightState.Fading:
                        component.CurrentState = ExpendableLightState.Dead;
                        var meta = MetaData(component.Owner);
                        meta.EntityName = component.SpentName;
                        meta.EntityDescription = component.SpentDesc;

                        UpdateSpriteAndSounds(component);
                        UpdateVisualizer(component);

                        if (TryComp<SharedItemComponent>(component.Owner, out var item))
                        {
                            item.EquippedPrefix = "unlit";
                        }

                        break;
                }
            }
        }

        /// <summary>
        ///     Enables the light if it is not active. Once active it cannot be turned off.
        /// </summary>
        public bool TryActivate(ExpendableLightComponent component)
        {
            if (!component.Activated && component.CurrentState == ExpendableLightState.BrandNew)
            {
                if (TryComp<SharedItemComponent>(component.Owner, out var item))
                {
                    item.EquippedPrefix = "lit";
                }

                component.CurrentState = ExpendableLightState.Lit;
                component.StateExpiryTime = component.GlowDuration;

                UpdateSpriteAndSounds(component);
                UpdateVisualizer(component);

                return true;
            }

            return false;
        }

        private void UpdateVisualizer(ExpendableLightComponent component, AppearanceComponent? appearance = null)
        {
            if (!Resolve(component.Owner, ref appearance, false)) return;

            appearance.SetData(ExpendableLightVisuals.State, component.CurrentState);

            switch (component.CurrentState)
            {
                case ExpendableLightState.Lit:
                    appearance.SetData(ExpendableLightVisuals.Behavior, component.TurnOnBehaviourID);
                    break;

                case ExpendableLightState.Fading:
                    appearance.SetData(ExpendableLightVisuals.Behavior, component.FadeOutBehaviourID);
                    break;

                case ExpendableLightState.Dead:
                    appearance.SetData(ExpendableLightVisuals.Behavior, string.Empty);
                    break;
            }
        }

        private void UpdateSpriteAndSounds(ExpendableLightComponent component)
        {
            if (TryComp<SpriteComponent>(component.Owner, out var sprite))
            {
                switch (component.CurrentState)
                {
                    case ExpendableLightState.Lit:
                    {
                        SoundSystem.Play(Filter.Pvs(component.Owner), component.LitSound.GetSound(), component.Owner);

                        if (component.IconStateLit != string.Empty)
                        {
                            sprite.LayerSetState(2, component.IconStateLit);
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
                        if (component.DieSound != null)
                            SoundSystem.Play(Filter.Pvs(component.Owner), component.DieSound.GetSound(), component.Owner);

                        sprite.LayerSetState(0, component.IconStateSpent);
                        sprite.LayerSetShader(0, "shaded");
                        sprite.LayerSetVisible(1, false);
                        break;
                    }
                }
            }

            if (TryComp<ClothingComponent>(component.Owner, out var clothing))
            {
                clothing.EquippedPrefix = component.Activated ? "Activated" : string.Empty;
            }
        }

        private void OnExpLightInit(EntityUid uid, ExpendableLightComponent component, ComponentInit args)
        {
            if (TryComp<SharedItemComponent?>(uid, out var item))
            {
                item.EquippedPrefix = "unlit";
            }

            component.CurrentState = ExpendableLightState.BrandNew;
            EntityManager.EnsureComponent<PointLightComponent>(uid);
        }

        private void OnExpLightUse(EntityUid uid, ExpendableLightComponent component, UseInHandEvent args)
        {
            if (args.Handled) return;

            if (TryActivate(component))
                args.Handled = true;
        }

        private void AddIgniteVerb(EntityUid uid, ExpendableLightComponent component, GetVerbsEvent<ActivationVerb> args)
        {
            if (!args.CanAccess || !args.CanInteract)
                return;

            if (component.CurrentState != ExpendableLightState.BrandNew)
                return;

            // Ignite the flare or make the glowstick glow.
            // Also hot damn, those are some shitty glowsticks, we need to get a refund.
            ActivationVerb verb = new()
            {
                Text = Loc.GetString("expendable-light-start-verb"),
                IconTexture = "/Textures/Interface/VerbIcons/light.svg.192dpi.png",
                Act = () => TryActivate(component)
            };
            args.Verbs.Add(verb);
        }
    }
}
