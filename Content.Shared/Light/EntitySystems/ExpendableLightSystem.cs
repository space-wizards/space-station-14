using Content.Shared.Abilities.Mime;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.IgnitionSource;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Light.Components;
using Content.Shared.NameModifier.EntitySystems;
using Content.Shared.Stacks;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Light.EntitySystems;

public sealed partial class ExpendableLightSystem : EntitySystem
{
    [Dependency] private SharedItemSystem _item = default!;
    [Dependency] private ClothingSystem _clothing = default!;
    [Dependency] private TagSystem _tagSystem = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedStackSystem _stackSystem = default!;
    [Dependency] private NameModifierSystem _nameModifier = default!;
    [Dependency] private IGameTiming _timing = default!;

    private static readonly ProtoId<TagPrototype> TrashTag = "Trash";

    /// <summary>
    ///     Checks if any light needs it's state updated.
    /// </summary>	
    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<ExpendableLightComponent>();
        while (query.MoveNext(out var uid, out var light))
        {
            if (light.Activated) // Only active lights can change state over time
                UpdateLight((uid, light));
        }
    }

    private void UpdateLight(Entity<ExpendableLightComponent> ent)
    {
        var component = ent.Comp;

        if (_timing.CurTime < component.StateExpiryTime) // Checks if any timer is expired
            return;

        switch (component.CurrentState) // State change time
        {
            case ExpendableLightState.Lit:
                component.CurrentState = ExpendableLightState.Fading;

                component.StateExpiryTime = _timing.CurTime + component.FadeOutDuration;
                UpdateVisualizer(ent);
                Dirty(ent);
                break;

            case ExpendableLightState.Fading:
            default:
                component.CurrentState = ExpendableLightState.Dead;
                component.StateExpiryTime = null;

                _nameModifier.RefreshNameModifiers(ent.Owner);
                _tagSystem.AddTag(ent, TrashTag);

                UpdateSounds(ent);
                UpdateVisualizer(ent);

                if (TryComp<ItemComponent>(ent, out var item))
                {
                    _item.SetHeldPrefix(ent, "unlit", component: item);
                }
                Dirty(ent);
                break;
        }
    }

    /// <summary>
    ///     Enables the light if it is not active. Once active it cannot be turned off.
    /// </summary>	   
    public bool TryActivate(Entity<ExpendableLightComponent> ent, EntityUid? user = null)
    {
        var component = ent.Comp;
        if (!component.Activated && component.CurrentState == ExpendableLightState.Unlit)
        {
            if (TryComp<ItemComponent>(ent, out var item))
            {
                _item.SetHeldPrefix(ent, "lit", component: item);
            }

            var ignite = new IgnitionEvent(true);
            RaiseLocalEvent(ent, ref ignite);

            component.CurrentState = ExpendableLightState.Lit;

            component.StateExpiryTime = _timing.CurTime + component.GlowDuration;

            UpdateSounds(ent, user);
            UpdateVisualizer(ent);
            Dirty(ent);
        }
        return true;
    }

    /// <summary>
    ///     Light refueling logic
    /// </summary>	
    [SubscribeLocalEvent]
    private void OnInteractUsing(EntityUid uid, ExpendableLightComponent component, ref InteractUsingEvent args)
    {
        if (args.Handled) return;

        if (!TryComp(args.Used, out StackComponent? stack)) return;

        if (stack.StackTypeId != component.RefuelMaterialID) return;

        switch (component.CurrentState)
        {
            case ExpendableLightState.Dead:
                component.CurrentState = ExpendableLightState.Unlit;
                _tagSystem.RemoveTag(uid, TrashTag);
                component.GlowDuration = component.RefuelMaterialTime;

                _nameModifier.RefreshNameModifiers(uid);
                UpdateVisualizer((uid, component));
                break;

            case ExpendableLightState.Unlit:
                if (component.GlowDuration + component.RefuelMaterialTime >= component.RefuelMaximumDuration) // light cannot hold more fuel
                    return;
                component.GlowDuration += component.RefuelMaterialTime;
                break;

            case ExpendableLightState.Fading:
                if (component.StateExpiryTime == null) return;
                var glowTimeLeft = component.StateExpiryTime.Value - _timing.CurTime; // how long until the light goes out
                if (glowTimeLeft + component.RefuelMaterialTime > component.FadeOutDuration) //enough fuel to go from fading into lit state
                {
                    var newGlowTime = glowTimeLeft + component.RefuelMaterialTime - component.FadeOutDuration;
                    component.CurrentState = ExpendableLightState.Lit;
                    component.StateExpiryTime = _timing.CurTime + newGlowTime;
                    UpdateVisualizer((uid, component));
                }
                else
                    component.StateExpiryTime += component.RefuelMaterialTime;
                break;

            case ExpendableLightState.Lit:
                if (component.StateExpiryTime == null) return;

                var timeLeft = component.StateExpiryTime.Value - _timing.CurTime;
                if (timeLeft + component.RefuelMaterialTime >= component.RefuelMaximumDuration) // light cannot hold more fuel
                    return;

                component.StateExpiryTime += component.RefuelMaterialTime;
                break;
        }
        Dirty(uid, component);
        _stackSystem.ReduceCount((args.Used, stack), 1);
        args.Handled = true;

    }

    [SubscribeLocalEvent]
    private void OnRefreshNameModifiers(Entity<ExpendableLightComponent> entity, ref RefreshNameModifiersEvent args)
    {
        if (entity.Comp.CurrentState is ExpendableLightState.Dead)
            args.AddModifier("expendable-light-spent-prefix");
    }

    private void UpdateVisualizer(Entity<ExpendableLightComponent> ent, AppearanceComponent? appearance = null)
    {
        var component = ent.Comp;
        if (!Resolve(ent, ref appearance, false))
            return;

        _appearance.SetData(ent, ExpendableLightVisuals.State, component.CurrentState, appearance);

        switch (component.CurrentState)
        {
            case ExpendableLightState.Lit:
                _appearance.SetData(ent, ExpendableLightVisuals.Behavior, component.TurnOnBehaviourID, appearance);
                break;

            case ExpendableLightState.Fading:
                _appearance.SetData(ent, ExpendableLightVisuals.Behavior, component.FadeOutBehaviourID, appearance);
                break;

            case ExpendableLightState.Dead:
                _appearance.SetData(ent, ExpendableLightVisuals.Behavior, string.Empty, appearance);
                var ignite = new IgnitionEvent(false);
                RaiseLocalEvent(ent, ref ignite);
                break;
        }
    }

    private void UpdateSounds(Entity<ExpendableLightComponent> ent, EntityUid? user = null)
    {
        var component = ent.Comp;

        switch (component.CurrentState)
        {
            case ExpendableLightState.Lit:
                _audio.PlayPredicted(component.LitSound, ent, user);

                if (component.PlayingStream == null && component.LoopedSound != null)
                {
                    var audioParams = component.LoopedSound.Params.WithLoop(true);
                    var stream = _audio.PlayPredicted(component.LoopedSound, ent, user, audioParams);
                    component.PlayingStream = stream?.Entity;
                }
                break;

            case ExpendableLightState.Fading:
                break;

            default:
                _audio.PlayPredicted(component.DieSound, ent, user);
                component.PlayingStream = _audio.Stop(component.PlayingStream);
                break;
        }

        if (TryComp<ClothingComponent>(ent, out var clothing))
        {
            _clothing.SetEquippedPrefix(ent, component.Activated ? "Activated" : string.Empty, clothing);
        }
    }

    [SubscribeLocalEvent]
    private void OnExpLightInit(EntityUid uid, ExpendableLightComponent component, ComponentInit args)
    {
        if (TryComp<ItemComponent>(uid, out var item))
        {
            _item.SetHeldPrefix(uid, "unlit", component: item);
        }
    }

    [SubscribeLocalEvent]
    private void OnExpLightUse(Entity<ExpendableLightComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (TryActivate(ent, args.User))
            args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void AddIgniteVerb(Entity<ExpendableLightComponent> ent, ref GetVerbsEvent<ActivationVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (ent.Comp.CurrentState != ExpendableLightState.Unlit)
            return;

        var user = args.User;
        ActivationVerb verb = new()
        {
            Text = Loc.GetString("expendable-light-start-verb"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/light.svg.192dpi.png")),
            Act = () => TryActivate(ent, user)
        };
        args.Verbs.Add(verb);
    }

    [SubscribeLocalEvent]
    private void OnLightShutdown(EntityUid uid, ExpendableLightComponent component, ComponentShutdown args)
    {
        component.PlayingStream = _audio.Stop(component.PlayingStream);
    }
}
