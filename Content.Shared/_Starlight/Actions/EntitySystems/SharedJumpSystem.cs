using System.Numerics;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared._Starlight.Actions.Components;
using Content.Shared._Starlight.Actions.Events;
using Content.Shared.Atmos.Components;
using Content.Shared.Throwing;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using Content.Shared.Stunnable;
using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;

namespace Content.Shared._Starlight.Actions.EntitySystems;

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
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedChargesSystem _chargesSystem = default!;

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
            _stun.TryKnockdown(uid, TimeSpan.FromSeconds(component.KnockdownSelfDuration), true);

        if (component.KnockdownTargetOnCollision)
            _stun.TryKnockdown(args.Target, TimeSpan.FromSeconds(component.KnockdownTargetDuration), true);
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

        OnJump(args);
    }

    private void OnJump(JumpActionEvent args)
    {
        if (args.Handled) return;

        var userTransform = Transform(args.Performer);
        var userMapCoords = _transform.GetMapCoordinates(userTransform);

        if (args.FromGrid && !_mapMan.TryFindGridAt(userMapCoords, out _, out _)) return;

        TryJump(args.Performer, args.Target, 15f, args.ToPointer, args.Sound, args.Distance);
        args.Handled = true;
    }

    public bool TryJump(Entity<JumpComponent?> ent, EntityCoordinates targetCoords, float speed = 15f, bool toPointer = false, SoundSpecifier? sound = null, float? distance = null, bool decreaseCharges = false)
    {
        if (!Resolve(ent, ref ent.Comp, false)
            || ent.Comp.ActionEntity == null
            || !TryComp<ActionComponent>(ent.Comp.ActionEntity, out var action)
            || _action.IsCooldownActive(action))
            return false;

        Jump(new Entity<JumpComponent>(ent, ent.Comp), targetCoords, speed, toPointer, sound, distance, decreaseCharges);
        return true;
    }

    public void Jump(Entity<JumpComponent> ent, EntityCoordinates targetCoords, float speed = 15f, bool toPointer = false, SoundSpecifier? sound = null, float? distance = null, bool decreaseCharges = false)
    {
        if (ent.Comp.ActionEntity == null
            || (TryComp<LimitedChargesComponent>(ent.Comp.ActionEntity, out var limitedCharges)
            && !_chargesSystem.HasCharges((ent.Comp.ActionEntity.Value, limitedCharges), 1)))
            return;
        else if (ent.Comp.ActionEntity != null && decreaseCharges)
            _chargesSystem.TryUseCharge(ent.Comp.ActionEntity.Value);
        var userTransform = Transform(ent.Owner);
        var userMapCoords = _transform.GetMapCoordinates(userTransform);
        var targetMapCoords = _transform.ToMapCoordinates(targetCoords);

        var vector = targetMapCoords.Position - userMapCoords.Position;
        if (distance != null
            && (!toPointer || Vector2.Distance(userMapCoords.Position, targetMapCoords.Position) > distance))
            vector = Vector2.Normalize(vector) * distance.Value;

        if (ent.Comp.ActionEntity != null && TryComp<ActionComponent>(ent.Comp.ActionEntity, out var action) && (limitedCharges == null || limitedCharges.MaxCharges <= 1))
            _action.SetCooldown((ent.Comp.ActionEntity.Value, action), TimeSpan.FromSeconds(ent.Comp.Cooldown));

        _throwing.TryThrow(ent.Owner, vector, baseThrowSpeed: speed, doSpin: false);

        if (sound != null)
            _audio.PlayPredicted(sound, ent.Owner, ent.Owner, AudioParams.Default.WithVolume(-4f));
    }
}