using Content.Server.Light.Components;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Light.Components;
using Content.Shared.Tag;
using Content.Shared.Temperature;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.Light.EntitySystems
{
    [UsedImplicitly]
    public sealed class ExpendableLightSystem : EntitySystem
    {
        [Dependency] private readonly SharedItemSystem _item = default!;
        [Dependency] private readonly ClothingSystem _clothing = default!;
        [Dependency] private readonly TagSystem _tagSystem = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly MetaDataSystem _metaData = default!;

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
                        _metaData.SetEntityName(component.Owner, Loc.GetString(component.SpentName), meta);
                        _metaData.SetEntityDescription(component.Owner, Loc.GetString(component.SpentDesc), meta);

                        _tagSystem.AddTag(component.Owner, "Trash");

                        UpdateSounds(component);
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

                UpdateSounds(component);
                UpdateVisualizer(component);

                return true;
            }

            return false;
        }

        private void UpdateVisualizer(ExpendableLightComponent component, AppearanceComponent? appearance = null)
        {
            if (!Resolve(component.Owner, ref appearance, false)) return;

            _appearance.SetData(appearance.Owner, ExpendableLightVisuals.State, component.CurrentState, appearance);

            switch (component.CurrentState)
            {
                case ExpendableLightState.Lit:
                    _appearance.SetData(appearance.Owner, ExpendableLightVisuals.Behavior, component.TurnOnBehaviourID, appearance);
                    break;

                case ExpendableLightState.Fading:
                    _appearance.SetData(appearance.Owner, ExpendableLightVisuals.Behavior, component.FadeOutBehaviourID, appearance);
                    break;

                case ExpendableLightState.Dead:
                    _appearance.SetData(appearance.Owner, ExpendableLightVisuals.Behavior, string.Empty, appearance);
                    var isHotEvent = new IsHotEvent() {IsHot = true};
                    RaiseLocalEvent(component.Owner, isHotEvent);
                    break;
            }
        }

        private void UpdateSounds(ExpendableLightComponent component)
        {
            var uid = component.Owner;

            switch (component.CurrentState)
            {
                case ExpendableLightState.Lit:
                    _audio.PlayPvs(component.LitSound, uid);
                    break;
                case ExpendableLightState.Fading:
                    break;
                default:
                    _audio.PlayPvs(component.DieSound, uid);
                    break;
            }

            if (TryComp<ClothingComponent>(uid, out var clothing))
            {
                _clothing.SetEquippedPrefix(uid, component.Activated ? "Activated" : string.Empty, clothing);
            }
        }

        private void OnExpLightInit(EntityUid uid, ExpendableLightComponent component, ComponentInit args)
        {
            if (TryComp<ItemComponent>(uid, out var item))
            {
                _item.SetHeldPrefix(uid, "unlit", item);
            }

            component.CurrentState = ExpendableLightState.BrandNew;
            EntityManager.EnsureComponent<PointLightComponent>(uid);
        }

        private void OnExpLightUse(EntityUid uid, ExpendableLightComponent component, UseInHandEvent args)
        {
            if (args.Handled) return;
            var isHotEvent = new IsHotEvent() {IsHot = true};
            RaiseLocalEvent(uid, isHotEvent);
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
                Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/light.svg.192dpi.png")),
                Act = () => TryActivate(component)
            };
            args.Verbs.Add(verb);
        }
    }
}
