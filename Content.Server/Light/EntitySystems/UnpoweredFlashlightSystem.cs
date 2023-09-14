using Content.Server.Light.Events;
using Content.Shared.Actions;
using Content.Shared.Decals;
using Content.Shared.Emag.Systems;
using Content.Shared.Light;
using Content.Shared.Light.Components;
using Content.Shared.Mind.Components;
using Content.Shared.Toggleable;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Light.EntitySystems
{
    public sealed class UnpoweredFlashlightSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
        [Dependency] private readonly SharedPointLightSystem _light = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<UnpoweredFlashlightComponent, GetVerbsEvent<ActivationVerb>>(AddToggleLightVerbs);
            SubscribeLocalEvent<UnpoweredFlashlightComponent, GetItemActionsEvent>(OnGetActions);
            SubscribeLocalEvent<UnpoweredFlashlightComponent, ToggleActionEvent>(OnToggleAction);
            SubscribeLocalEvent<UnpoweredFlashlightComponent, MindAddedMessage>(OnMindAdded);
            SubscribeLocalEvent<UnpoweredFlashlightComponent, GotEmaggedEvent>(OnGotEmagged);
        }

        private void OnToggleAction(EntityUid uid, UnpoweredFlashlightComponent component, ToggleActionEvent args)
        {
            if (args.Handled)
                return;

            ToggleLight(uid, component);

            args.Handled = true;
        }

        private void OnGetActions(EntityUid uid, UnpoweredFlashlightComponent component, GetItemActionsEvent args)
        {
            args.AddAction(ref component.ToggleActionEntity, component.ToggleAction);
        }

        private void AddToggleLightVerbs(EntityUid uid, UnpoweredFlashlightComponent component, GetVerbsEvent<ActivationVerb> args)
        {
            if (!args.CanAccess || !args.CanInteract)
                return;

            ActivationVerb verb = new()
            {
                Text = Loc.GetString("toggle-flashlight-verb-get-data-text"),
                Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/light.svg.192dpi.png")),
                Act = () => ToggleLight(uid, component),
                Priority = -1 // For things like PDA's, Open-UI and other verbs that should be higher priority.
            };

            args.Verbs.Add(verb);
        }

        private void OnMindAdded(EntityUid uid, UnpoweredFlashlightComponent component, MindAddedMessage args)
        {
            _actionsSystem.AddAction(uid, ref component.ToggleActionEntity, component.ToggleAction);
        }

        private void OnGotEmagged(EntityUid uid, UnpoweredFlashlightComponent component, ref GotEmaggedEvent args)
        {
            if (!_light.TryGetLight(uid, out var light))
                return;

            if (_prototypeManager.TryIndex<ColorPalettePrototype>(component.EmaggedColorsPrototype, out var possibleColors))
            {
                var pick = _random.Pick(possibleColors.Colors.Values);
                _light.SetColor(uid, pick, light);
            }

            args.Repeatable = true;
            args.Handled = true;
        }

        public void ToggleLight(EntityUid uid, UnpoweredFlashlightComponent flashlight)
        {
            if (!_light.TryGetLight(uid, out var light))
                return;

            flashlight.LightOn = !flashlight.LightOn;
            _light.SetEnabled(uid, flashlight.LightOn, light);

            _appearance.SetData(uid, UnpoweredFlashlightVisuals.LightOn, flashlight.LightOn);

            _audioSystem.PlayPvs(flashlight.ToggleSound, uid);

            RaiseLocalEvent(uid, new LightToggleEvent(flashlight.LightOn), true);
            _actionsSystem.SetToggled(flashlight.ToggleActionEntity, flashlight.LightOn);
        }
    }
}
