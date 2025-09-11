using System;
using System.Linq;
using System.Numerics;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Atmos.Components;
using Content.Shared.Throwing;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Content.Shared.Stunnable;

namespace Content.Shared._Starlight.Actions.Jump;

//idea taked from VigersRay
public abstract class SharedJumpSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IMapManager _mapMan = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<JumpComponent, MapInitEvent>(OnStartup);
        SubscribeLocalEvent<JumpComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<JumpComponent, JetJumpActionEvent>(OnJump);
        SubscribeLocalEvent<JumpComponent, GetItemActionsEvent>(OnGetItemActions);
        SubscribeLocalEvent<JumpComponent, ThrowDoHitEvent>(OnThrowCollide);
        SubscribeLocalEvent<JumpActionEvent>(OnJump);
    }

    private void OnThrowCollide(EntityUid uid, JumpComponent component, ref ThrowDoHitEvent args)
    {
        if (component.KnockdownSelfOnCollision)
            _stun.TryKnockdown(uid, TimeSpan.FromSeconds(2), true);

        if (component.KnockdownTargetOnCollision)
            _stun.TryKnockdown(args.Target, TimeSpan.FromSeconds(2), true);
    }

    private void OnGetItemActions(Entity<JumpComponent> ent, ref GetItemActionsEvent args)
    {
        if (ent.Comp.IsEquipment)
            args.AddAction(ref ent.Comp.ActionEntity, ent.Comp.Action);
    }

    private void OnStartup(EntityUid uid, JumpComponent component, MapInitEvent args)
    {
        if (component.IsEquipment)
        {
            if (_actionContainer.EnsureAction(uid, ref component.ActionEntity, out var action, component.Action))
                _action.SetEntityIcon((component.ActionEntity.Value, action), uid);
        }
        else
            _action.AddAction(uid, ref component.ActionEntity, component.Action);

        Dirty(uid, component);
    }

    private void OnShutdown(EntityUid uid, JumpComponent component, ComponentShutdown args)
    {
        if (Deleted(uid) || component.ActionEntity is null)
            return;

        if (component.IsEquipment)
            _actionContainer.RemoveAction(component.ActionEntity.Value);
        else
            _action.RemoveAction((uid, null), component.ActionEntity);
    }

    protected virtual bool TryReleaseGas(Entity<JumpComponent> ent, ref JetJumpActionEvent args)
        => TryComp<GasTankComponent>(ent, out var gasTank) && gasTank.TotalMoles > args.MoleUsage;

    private void OnJump(Entity<JumpComponent> ent, ref JetJumpActionEvent args)
    {
        if (args.Handled
            || !TryReleaseGas(ent, ref args))
            return;

        OnJump((JumpActionEvent)args);
    }

    private void OnJump(JumpActionEvent args)
    {
        if (args.Handled) return;

        var userTransform = Transform(args.Performer);
        var userMapCoords = _transform.GetMapCoordinates(userTransform);

        if (args.FromGrid && !_mapMan.TryFindGridAt(userMapCoords, out _, out _)) return;
        args.Handled = true;

        var targetMapCoords = _transform.ToMapCoordinates(args.Target);

        var vector = targetMapCoords.Position - userMapCoords.Position;
        if (!args.ToPointer
            || Vector2.Distance(userMapCoords.Position, targetMapCoords.Position) > args.Distance)
            vector = Vector2.Normalize(vector) * args.Distance;

        _throwing.TryThrow(args.Performer, vector, baseThrowSpeed: args.Speed, doSpin: false);

        _audio.PlayPredicted(args.Sound, args.Performer, args.Performer, AudioParams.Default.WithVolume(-4f));
    }

    public bool TryJump(Entity<JumpComponent?> ent, EntityCoordinates targetCoords, float speed = 15f, bool toPointer = false, SoundSpecifier? sound = null, float? distance = null)
    {
        if (!Resolve(ent, ref ent.Comp)
            || ent.Comp.ActionEntity == null
            || !TryComp<ActionComponent>(ent.Comp.ActionEntity, out var action)
            || _action.IsCooldownActive(action))
            return false;

        Jump(new Entity<JumpComponent>(ent, ent.Comp), targetCoords, speed, toPointer, sound, distance);
        return true;
    }

    public void Jump(Entity<JumpComponent> ent, EntityCoordinates targetCoords, float speed = 15f, bool toPointer = false, SoundSpecifier? sound = null, float? distance = null)
    {
        var userTransform = Transform(ent.Owner);
        var userMapCoords = _transform.GetMapCoordinates(userTransform);
        var targetMapCoords = _transform.ToMapCoordinates(targetCoords);

        var vector = targetMapCoords.Position - userMapCoords.Position;
        if (distance != null
            && (!toPointer || Vector2.Distance(userMapCoords.Position, targetMapCoords.Position) > distance))
            vector = Vector2.Normalize(vector) * distance.Value;

        _throwing.TryThrow(ent.Owner, vector, baseThrowSpeed: speed, doSpin: false);

        if (sound != null)
            _audio.PlayPredicted(sound, ent.Owner, ent.Owner, AudioParams.Default.WithVolume(-4f));
    }
}