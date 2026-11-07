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
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Light.EntitySystems;

public sealed partial class ExpendableLightSystem : EntitySystem
{
    [Dependency] private SharedItemSystem _item = default!;
    [Dependency] private TagSystem _tagSystem = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedStackSystem _stackSystem = default!;
    [Dependency] private NameModifierSystem _nameModifier = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;

    [Dependency] private EntityQuery<StackComponent> _stackQuery;
    [Dependency] private EntityQuery<AppearanceComponent> _appearanceQuery;

    private static readonly ProtoId<TagPrototype> TrashTag = "Trash";

    /// <summary>
    /// Checks if any light needs its state updated.
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

    /// <summary>
    /// Light refueling logic
    /// </summary>
    [SubscribeLocalEvent]
    private void OnInteractUsing(Entity<ExpendableLightComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!_stackQuery.TryComp(args.Used, out var stack))
            return;

        if (stack.StackTypeId != ent.Comp.RefuelMaterialID)
            return;

        var fuelTime = ent.Comp.RefuelMaterialTime;
        if (fuelTime <= TimeSpan.Zero)
            return;

        var remainingGlowTime = ent.Comp.CurrentState switch
        {
            ExpendableLightState.Unlit => ent.Comp.GlowDuration,
            ExpendableLightState.Lit => ent.Comp.StateExpiryTime - _timing.CurTime,
            ExpendableLightState.Fading => ent.Comp.StateExpiryTime - _timing.CurTime - ent.Comp.FadeOutDuration,
            ExpendableLightState.Dead => TimeSpan.Zero,
            _ => ent.Comp.RefuelMaximumDuration,
        };

        // Fading has already consumed part of the fade-out reserve before restoring full brightness.
        if (remainingGlowTime + fuelTime > ent.Comp.RefuelMaximumDuration
            || !_stackSystem.TryUse((args.Used, stack), 1))
            return;

        switch (ent.Comp.CurrentState)
        {
            case ExpendableLightState.Dead:
                ent.Comp.CurrentState = ExpendableLightState.Unlit;
                _tagSystem.RemoveTag(ent, TrashTag);
                ent.Comp.GlowDuration = fuelTime;

                _nameModifier.RefreshNameModifiers(ent.Owner);
                UpdateVisualizer(ent);
                break;

            case ExpendableLightState.Unlit:
                ent.Comp.GlowDuration += fuelTime;
                break;

            case ExpendableLightState.Fading:
                var newGlowTime = remainingGlowTime + fuelTime;
                if (newGlowTime > TimeSpan.Zero)
                {
                    ent.Comp.CurrentState = ExpendableLightState.Lit;
                    ent.Comp.StateExpiryTime = _timing.CurTime + newGlowTime;
                    UpdateVisualizer(ent);
                }
                else
                    ent.Comp.StateExpiryTime += fuelTime;
                break;

            case ExpendableLightState.Lit:
                ent.Comp.StateExpiryTime += fuelTime;
                break;
        }
        Dirty(ent);
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnRefreshNameModifiers(Entity<ExpendableLightComponent> entity, ref RefreshNameModifiersEvent args)
    {
        if (entity.Comp.CurrentState is ExpendableLightState.Dead)
            args.AddModifier("expendable-light-spent-prefix");
    }

    [SubscribeLocalEvent]
    private void OnExpLightStartup(Entity<ExpendableLightComponent> ent, ref ComponentStartup args)
    {
        _item.SetHeldPrefix(ent, ent.Comp.Activated ? "lit" : "unlit");
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
    private void OnLightShutdown(Entity<ExpendableLightComponent> ent, ref ComponentShutdown args)
    {
        ent.Comp.PlayingStream = _audio.Stop(ent.Comp.PlayingStream);
    }

    /// <summary>
    /// Enables the light if it is not active. Once active it cannot be turned off.
    /// </summary>
    public bool TryActivate(Entity<ExpendableLightComponent> ent, EntityUid user)
    {
        if (ent.Comp.CurrentState != ExpendableLightState.Unlit)
            return false;

        _item.SetHeldPrefix(ent, "lit");

        var ignite = new IgnitionEvent(true);
        RaiseLocalEvent(ent, ref ignite);

        ent.Comp.CurrentState = ExpendableLightState.Lit;
        ent.Comp.StateExpiryTime = _timing.CurTime + ent.Comp.GlowDuration;

        PlayIgniteSound(ent, user);
        UpdateVisualizer(ent);
        Dirty(ent);
        return true;
    }

    private void UpdateLight(Entity<ExpendableLightComponent> ent)
    {
        if (_timing.CurTime < ent.Comp.StateExpiryTime)
            return;

        switch (ent.Comp.CurrentState) // State change time
        {
            case ExpendableLightState.Lit:
                ent.Comp.CurrentState = ExpendableLightState.Fading;
                ent.Comp.StateExpiryTime += ent.Comp.FadeOutDuration;

                if (_timing.CurTime < ent.Comp.StateExpiryTime)
                {
                    UpdateVisualizer(ent);
                    break;
                }

                goto case ExpendableLightState.Fading;

            case ExpendableLightState.Fading:
                ent.Comp.CurrentState = ExpendableLightState.Dead;
                ent.Comp.StateExpiryTime = TimeSpan.Zero;

                var ignite = new IgnitionEvent(false);
                RaiseLocalEvent(ent, ref ignite);

                _nameModifier.RefreshNameModifiers(ent.Owner);
                _tagSystem.AddTag(ent, TrashTag);

                PlayDieOutSound(ent);
                UpdateVisualizer(ent);

                _item.SetHeldPrefix(ent, "unlit");
                break;

            default:
                return;
        }

        Dirty(ent);
    }

    private void UpdateVisualizer(Entity<ExpendableLightComponent> ent)
    {
        if (!_appearanceQuery.TryComp(ent, out var appearance))
            return;

        _appearance.SetData(ent, ExpendableLightVisuals.State, ent.Comp.CurrentState, appearance);

        switch (ent.Comp.CurrentState)
        {
            case ExpendableLightState.Lit:
                _appearance.SetData(ent, ExpendableLightVisuals.Behavior, ent.Comp.TurnOnBehaviourID, appearance);
                break;

            case ExpendableLightState.Fading:
                _appearance.SetData(ent, ExpendableLightVisuals.Behavior, ent.Comp.FadeOutBehaviourID, appearance);
                break;

            case ExpendableLightState.Dead:
            case ExpendableLightState.Unlit:
                _appearance.SetData(ent, ExpendableLightVisuals.Behavior, string.Empty, appearance);
                break;
        }
    }

    private void PlayDieOutSound(Entity<ExpendableLightComponent> ent)
    {
        if (_net.IsServer)
            _audio.PlayPvs(ent.Comp.DieSound, ent);

        ent.Comp.PlayingStream = _audio.Stop(ent.Comp.PlayingStream);
    }

    private void PlayIgniteSound(Entity<ExpendableLightComponent> ent, EntityUid user)
    {
        _audio.PlayPredicted(ent.Comp.LitSound, ent, user);

        if (ent.Comp.PlayingStream != null || ent.Comp.LoopedSound == null)
            return;

        var audioParams = ent.Comp.LoopedSound.Params.WithLoop(true);
        var stream = _audio.PlayPredicted(ent.Comp.LoopedSound, ent, user, audioParams);
        ent.Comp.PlayingStream = stream?.Entity;
    }
}
