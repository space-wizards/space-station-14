
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.Coordinates;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;
using Content.Shared.Popups;
using Robust.Shared.Timing;
using Robust.Shared.Network;


namespace Content.Shared._Impstation.Mailinator;

public sealed class MailinatorSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MailinatorComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<MailinatorComponent, MailinatorDoAfterEvent>(OnMailinatorDoAfter);
    }

    /// <summary>
    /// Adds the verb for teleporting the item to the nearest ent with MailinatorTargetComponent.
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="args"></param>
    private void OnGetVerbs(Entity<MailinatorComponent> entity, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        var user = args.User;
        args.Verbs.Add(new AlternativeVerb
        {
            Text = entity.Comp.VerbText,
            Priority = 100,
            Act = () =>
            {
                var doAfter = new DoAfterArgs(EntityManager, user, entity.Comp.DoAfterLength, new MailinatorDoAfterEvent(), entity.Owner)
                {
                    BreakOnMove = true,
                    BlockDuplicate = true,
                    BreakOnDamage = true,
                    CancelDuplicate = true
                };

                _doAfterSystem.TryStartDoAfter(doAfter);
            }
        });
    }

    private void OnMailinatorDoAfter(Entity<MailinatorComponent> entity, ref MailinatorDoAfterEvent args)
    {
        var mailinatorCoords = _transformSystem.GetMapCoordinates(entity.Owner);

        if (!_net.IsServer)
            return;

        if (!_timing.IsFirstTimePredicted)
            return;

        if (TryGetNearestMailTelepad(mailinatorCoords, out var target, out var targetCoords) && TeleportEntity(entity, targetCoords.Value))
        {
            Spawn(entity.Comp.BeamInFx, Transform(entity.Owner).Coordinates);
            _audio.PlayPredicted(entity.Comp.DepartureSound, entity.Owner, args.User);

            Spawn(entity.Comp.BeamInFx, Transform(target.Value.Owner).Coordinates);
            _audio.PlayPredicted(entity.Comp.ArrivalSound, target.Value.Owner, args.User);
        }
        else
        {
            _popup.PopupEntity("No valid destinations in range.", args.User);
        }
    }


    private bool TeleportEntity(Entity<MailinatorComponent> entity, EntityCoordinates target)
    {
        var mailinatorCoords = Transform(entity.Owner).Coordinates;
        var onSameMap = _transformSystem.GetMapId(mailinatorCoords) == _transformSystem.GetMapId(target);

        if (!onSameMap)
            return false;

        _transformSystem.SetCoordinates(entity.Owner, target);
        return true;
    }

    public bool TryGetNearestMailTelepad(MapCoordinates coordinates, [NotNullWhen(true)] out Entity<MailinatorTargetComponent>? target, [NotNullWhen(true)] out EntityCoordinates? targetCoords)
    {
        target = null;
        targetCoords = null;
        var minDistance = float.PositiveInfinity;

        var enumerator = EntityQueryEnumerator<MailinatorTargetComponent, TransformComponent>();
        while (enumerator.MoveNext(out var uid, out var targetComp, out var xform))
        {
            if (coordinates.MapId != xform.MapID)
                continue;

            var coords = _transformSystem.GetWorldPosition(xform);
            var distanceSquared = (coordinates.Position - coords).LengthSquared();
            if (!float.IsInfinity(minDistance) && distanceSquared >= minDistance)
                continue;

            minDistance = distanceSquared;
            target = (uid, targetComp);
            targetCoords = new EntityCoordinates(target.Value, target.Value.Owner.ToCoordinates().Position);
        }

        return target != null;
    }
}

/// <summary>
/// Is relayed after the doafter finishes.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class MailinatorDoAfterEvent : SimpleDoAfterEvent { }
