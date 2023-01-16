using System.Linq;
using System.Threading;
using Content.Server.Construction.Components;
using Content.Server.DoAfter;
using Content.Server.Storage.Components;
using Content.Server.Storage.EntitySystems;
using Content.Server.Wires;
using Content.Shared.Construction.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.Construction;

public sealed class PartExchangerSystem : EntitySystem
{
    [Dependency] private readonly ConstructionSystem _construction = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly StorageSystem _storage = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<PartExchangerComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<PartExchangerComponent, RpedExchangeFinishedEvent>(OnFinished);
        SubscribeLocalEvent<PartExchangerComponent, RpedExchangeCancelledEvent>(OnCancelled);
    }

    private void OnFinished(EntityUid uid, PartExchangerComponent component, RpedExchangeFinishedEvent args)
    {
        component.Token = null;
        component.AudioStream?.Stop();

        if (!TryComp<MachineComponent>(args.Target, out var machine))
            return;

        if (!TryComp<ServerStorageComponent>(uid, out var storage) || storage.Storage == null)
            return; //the parts are stored in here

        var board = machine.BoardContainer.ContainedEntities.FirstOrNull();

        if (board == null || !TryComp<MachineBoardComponent>(board, out var macBoardComp))
            return;

        var machineParts = new List<MachinePartComponent>();

        foreach (var ent in storage.Storage.ContainedEntities) //get parts in RPED
        {
            if (TryComp<MachinePartComponent>(ent, out var part))
                machineParts.Add(part);
        }
        foreach (var ent in new List<EntityUid>(machine.PartContainer.ContainedEntities)) //clone so don't modify during enumeration
        {
            if (TryComp<MachinePartComponent>(ent, out var part))
            {
                machineParts.Add(part);
                _container.RemoveEntity(machine.Owner, ent);
            }
        }

        //order by highest rating
        machineParts = machineParts.OrderByDescending(p => p.Rating).ToList();

        var updatedParts = new List<MachinePartComponent>();
        foreach (var (type, amount) in macBoardComp.Requirements)
        {
            var target = machineParts.Where(p => p.PartType == type).Take(amount);
            updatedParts.AddRange(target);
        }
        foreach (var part in updatedParts)
        {
            machine.PartContainer.Insert(part.Owner, EntityManager);
            machineParts.Remove(part);
        }

        //put the unused parts back into rped. (this also does the "swapping")
        foreach (var unused in machineParts)
        {
            storage.Storage.Insert(unused.Owner);
            _storage.Insert(uid, unused.Owner, null, false);
        }
        _construction.RefreshParts(machine);
    }

    private void OnCancelled(EntityUid uid, PartExchangerComponent component, RpedExchangeCancelledEvent args)
    {
        component.Token = null;
        component.AudioStream?.Stop();
    }

    private void OnAfterInteract(EntityUid uid, PartExchangerComponent component, AfterInteractEvent args)
    {
        if (component.Token != null)
            return;

        if (component.DoDistanceCheck && !args.CanReach)
            return;

        if (args.Target == null)
            return;

        if (!HasComp<MachineComponent>(args.Target))
            return;

        if (TryComp<WiresComponent>(args.Target, out var wires) && !wires.IsPanelOpen)
        {
            _popup.PopupEntity(Loc.GetString("construction-step-condition-wire-panel-open"),
                args.Target.Value);
            return;
        }

        component.AudioStream = _audio.PlayPvs(component.ExchangeSound, uid);

        component.Token = new CancellationTokenSource();
        _doAfter.DoAfter(new DoAfterEventArgs(args.User, component.ExchangeDuration, component.Token.Token, args.Target, args.Used)
        {
            BreakOnDamage = true,
            BreakOnStun = true,
            BreakOnUserMove = true,
            UsedFinishedEvent = new RpedExchangeFinishedEvent(args.Target.Value),
            UsedCancelledEvent = new RpedExchangeCancelledEvent()
        });
    }
}

public sealed class RpedExchangeFinishedEvent : EntityEventArgs
{
    public readonly EntityUid Target;

    public RpedExchangeFinishedEvent(EntityUid target)
    {
        Target = target;
    }
}

public readonly struct RpedExchangeCancelledEvent
{
}
