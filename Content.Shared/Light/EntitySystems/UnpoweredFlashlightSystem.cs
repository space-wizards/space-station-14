using Content.Shared.Actions;
using Content.Shared.Emag.Systems;
using Content.Shared.Light.Components;
using Content.Shared.Mind.Components;
using Content.Shared.Toggleable;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Shared.Light.EntitySystems;

public sealed class UnpoweredFlashlightSystem : EntitySystem
{
    // TODO: Split some of this to ItemTogglePointLight

    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
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
        SubscribeLocalEvent<UnpoweredFlashlightComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, UnpoweredFlashlightComponent component, MapInitEvent args)
    {
        _actionContainer.EnsureAction(uid, ref component.ToggleActionEntity, component.ToggleAction);
        Dirty(uid, component);
    }

    private void OnToggleAction(EntityUid uid, UnpoweredFlashlightComponent component, ToggleActionEvent args)
    {
        if (args.Handled)
            return;

        TryToggleLight((uid, component), args.Performer);
        args.Handled = true;
    }

    private void OnGetActions(EntityUid uid, UnpoweredFlashlightComponent component, GetItemActionsEvent args)
    {
        args.AddAction(component.ToggleActionEntity);
    }

    private void AddToggleLightVerbs(EntityUid uid, UnpoweredFlashlightComponent component, GetVerbsEvent<ActivationVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        ActivationVerb verb = new()
        {
            Text = Loc.GetString("toggle-flashlight-verb-get-data-text"),
            Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/light.svg.192dpi.png")),
            Act = () => TryToggleLight((uid, component), args.User),
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

        if (_prototypeManager.TryIndex(component.EmaggedColorsPrototype, out var possibleColors))
        {
            var pick = _random.Pick(possibleColors.Colors.Values);
            _light.SetColor(uid, pick, light);
        }

        args.Repeatable = true;
        args.Handled = true;
    }

    public void TryToggleLight(Entity<UnpoweredFlashlightComponent?> ent, EntityUid? user = null, bool quiet = false)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        SetLight(ent, !ent.Comp.LightOn, user, quiet);
    }

    public void SetLight(Entity<UnpoweredFlashlightComponent?> ent, bool value, EntityUid? user = null, bool quiet = false)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (ent.Comp.LightOn == value)
            return;

        if (!_light.TryGetLight(ent, out var light))
            return;

        Dirty(ent);
        ent.Comp.LightOn = value;
        _light.SetEnabled(ent, value, light);
        _appearance.SetData(ent, UnpoweredFlashlightVisuals.LightOn, value);

        if (!quiet)
            _audioSystem.PlayPredicted(ent.Comp.ToggleSound, ent, user);

        _actionsSystem.SetToggled(ent.Comp.ToggleActionEntity, value);
        RaiseLocalEvent(ent, new LightToggleEvent(value));
    }
}
