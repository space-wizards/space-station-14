using Content.Shared.Actions;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Item;
using Content.Shared.Light;
using Content.Shared.Light.Components;
using Content.Shared.Toggleable;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.Light;

public abstract class SharedHandheldLightSystem : EntitySystem
{
    [Dependency] private readonly SharedItemSystem _itemSys = default!;
    [Dependency] private readonly ClothingSystem _clothingSys = default!;
    [Dependency] private readonly SharedActionsSystem _actionSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HandheldLightComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<HandheldLightComponent, ComponentHandleState>(OnHandleState);

        SubscribeLocalEvent<HandheldLightComponent, GetVerbsEvent<ActivationVerb>>(AddToggleLightVerb);
    }

    private void OnInit(EntityUid uid, HandheldLightComponent component, ComponentInit args)
    {
        UpdateVisuals(uid, component);

        // Want to make sure client has latest data on level so battery displays properly.
        Dirty(uid, component);
    }

    private void OnHandleState(EntityUid uid, HandheldLightComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not HandheldLightComponent.HandheldLightComponentState state)
            return;

        component.Level = state.Charge;
        SetActivated(uid, state.Activated, component, false);
    }

    public void SetActivated(EntityUid uid, bool activated, HandheldLightComponent? component = null, bool makeNoise = true)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.Activated == activated)
            return;

        component.Activated = activated;

        if (makeNoise)
        {
            var sound = component.Activated ? component.TurnOnSound : component.TurnOffSound;
            _audio.PlayPvs(sound, uid);
        }

        Dirty(uid, component);
        UpdateVisuals(uid, component);

        var ev = new LightToggleEvent(activated);
        RaiseLocalEvent(uid, ev);
    }

    public void UpdateVisuals(EntityUid uid, HandheldLightComponent? component = null, AppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref component, ref appearance, false))
            return;

        if (component.AddPrefix)
        {
            var prefix = component.Activated ? "on" : "off";
            _itemSys.SetHeldPrefix(uid, prefix);
            _clothingSys.SetEquippedPrefix(uid, prefix);
        }

        if (component.ToggleActionEntity != null)
            _actionSystem.SetToggled(component.ToggleActionEntity, component.Activated);

        _appearance.SetData(uid, ToggleableVisuals.Enabled, component.Activated, appearance);
    }

    private void AddToggleLightVerb(Entity<HandheldLightComponent> ent, ref GetVerbsEvent<ActivationVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !ent.Comp.ToggleOnInteract)
            return;

        var @event = args;
        ActivationVerb verb = new()
        {
            Text = Loc.GetString("verb-common-toggle-light"),
            Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/light.svg.192dpi.png")),
            Act = ent.Comp.Activated
                ? () => TurnOff(ent)
                : () => TurnOn(@event.User, ent)
        };

        args.Verbs.Add(verb);
    }

    public abstract bool TurnOff(Entity<HandheldLightComponent> ent, bool makeNoise = true);
    public abstract bool TurnOn(EntityUid user, Entity<HandheldLightComponent> uid);
}
