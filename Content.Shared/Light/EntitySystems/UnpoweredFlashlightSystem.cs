using Content.Shared.ActionBlocker;
using Content.Shared.Emag.Systems;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Light.Components;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Shared.Light.EntitySystems;

public sealed partial class UnpoweredFlashlightSystem : EntitySystem
{
    // TODO: Split some of this to ItemTogglePointLight

    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedAudioSystem _audioSystem = default!;
    [Dependency] private SharedPointLightSystem _light = default!;
    [Dependency] private EmagSystem _emag = default!;
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private SharedHandheldLightSystem _handheldLight = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UnpoweredFlashlightComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<UnpoweredFlashlightComponent, GetVerbsEvent<ActivationVerb>>(AddToggleLightVerbs);
        SubscribeLocalEvent<UnpoweredFlashlightComponent, GotEmaggedEvent>(OnGotEmagged);

        SubscribeAllEvent<ToggleFlashlightEvent>(OnToggleFlashlight);
    }

    private void OnToggleFlashlight(ToggleFlashlightEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } ent ||
            !TryGetEntity(msg.Flashlight, out var flashlight))
            return;

        if (!_interaction.IsAccessible(ent, flashlight.Value) ||
            !_actionBlocker.CanInteract(ent, flashlight.Value))
            return;

        TryToggleLight(flashlight.Value, ent);
        _handheldLight.TryToggleLight(flashlight.Value, ent);
    }

    private void OnExamine(Entity<UnpoweredFlashlightComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("flashlight-toggle-examine-keybind"));
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

    private void OnGotEmagged(EntityUid uid, UnpoweredFlashlightComponent component, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        if (!_light.TryGetLight(uid, out var light))
            return;

        if (ProtoMan.Resolve(component.EmaggedColorsPrototype, out var possibleColors))
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

        RaiseLocalEvent(ent, new LightToggleEvent(value));
    }
}
