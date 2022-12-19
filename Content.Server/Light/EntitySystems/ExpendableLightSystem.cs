using Content.Server.Light.Components;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Light.Component;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Server.Light.EntitySystems
{
    [UsedImplicitly]
    public sealed class ExpendableLightSystem : EntitySystem
    {
        [Dependency] private readonly SharedItemSystem _item = default!;
        [Dependency] private readonly ClothingSystem _clothing = default!;
        [Dependency] private readonly TagSystem _tagSystem = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;

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
                        meta.EntityName = Loc.GetString(component.SpentName);
                        meta.EntityDescription = Loc.GetString(component.SpentDesc);

                        _tagSystem.AddTag(component.Owner, "Trash");

                        UpdateSpriteAndSounds(component);
                        UpdateVisualizer(component);

                        if (TryComp<ItemComponent>(component.Owner, out var item))
                        {
                            _item.SetHeldPrefix(component.Owner, "unlit", item);
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
                if (TryComp<ItemComponent>(component.Owner, out var item))
                {
                    _item.SetHeldPrefix(component.Owner, "lit", item);
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
                        _audio.PlayPvs(component.LitSound, component.Owner);
                        if (component.IconStateLit != string.Empty)
                        {
                            sprite.LayerSetState(2, component.IconStateLit);
                            sprite.LayerSetShader(2, "shaded");
                        }

                        sprite.LayerSetVisible(1, true);
                        break;
                    case ExpendableLightState.Fading:
                    {
                        break;
                    }
                    default:
                    case ExpendableLightState.Dead:
                        _audio.PlayPvs(component.DieSound, component.Owner);
                        if (!string.IsNullOrEmpty(component.IconStateSpent))
                        {
                            sprite.LayerSetState(0, component.IconStateSpent);
                            sprite.LayerSetShader(0, "shaded");
                        }

                        sprite.LayerSetVisible(1, false);
                        break;
                }
            }

            if (TryComp<ClothingComponent>(component.Owner, out var clothing))
            {
                _clothing.SetEquippedPrefix(component.Owner, component.Activated ? "Activated" : string.Empty, clothing);
            }
        }

        private void OnExpLightInit(EntityUid uid, ExpendableLightComponent component, ComponentInit args)
        {
            if (TryComp<ItemComponent?>(uid, out var item))
            {
                _item.SetHeldPrefix(uid, "unlit", item);
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
