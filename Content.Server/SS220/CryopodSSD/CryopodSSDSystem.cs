using Content.Server.Mind.Components;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.SS220.CryopodSSD;
using Content.Shared.Verbs;
using Robust.Shared.Timing;

namespace Content.Server.SS220.CryopodSSD;


/// <summary>
/// SS220
/// Implemented leaving from game via climbing in cryopod
/// <seealso cref="CryopodSSDComponent"/>
/// </summary>
public sealed class CryopodSSDSystem : SharedCryopodSSDSystem
{
    [Dependency] private readonly SSDStorageConsoleSystem _SSDStorageConsoleSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = Logger.GetSawmill("cryopodSSD");

        SubscribeLocalEvent<CryopodSSDComponent, ComponentInit>(OnComponentInit);
        
        SubscribeLocalEvent<CryopodSSDComponent, GetVerbsEvent<AlternativeVerb>>(AddAlternativeVerbs);
        SubscribeLocalEvent<CryopodSSDComponent, CryopodSSDLeaveActionEvent>(OnCryopodSSDLeaveAction);
        
        SubscribeLocalEvent<CryopodSSDComponent, CryopodSSDDragFinished>(OnDragFinished);
        SubscribeLocalEvent<CryopodSSDComponent, DragDropTargetEvent>(HandleDragDropOn);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var currTime = _gameTiming.CurTime;

        var entityEnumerator = EntityQueryEnumerator<CryopodSSDComponent>();

        while (entityEnumerator.MoveNext(out var uid, out var cryopodSSDComp))
        {
            if (cryopodSSDComp.BodyContainer.ContainedEntity is null ||
                currTime < cryopodSSDComp.CurrentEntityLyingInCryopodTime +
                TimeSpan.FromSeconds(cryopodSSDComp.AutoTransferDelay))
            {
                continue;
            }

            TransferToCryoStorage(uid, cryopodSSDComp);           
        }
    }

    /// <summary>
    /// Ejects body from cryopod
    /// </summary>
    /// <param name="uid"> EntityUid of the cryopod</param>
    /// <param name="cryopodSsdComponent"></param>
    /// <returns> EntityUid of the ejected body if it succeeded, otherwise returns null</returns>
    public override EntityUid? EjectBody(EntityUid uid, CryopodSSDComponent? cryopodSsdComponent)
    {
        if (!Resolve(uid, ref cryopodSsdComponent))
        {
            return null;
        }

        if (cryopodSsdComponent.BodyContainer.ContainedEntity is not { Valid: true } contained)
        {
            return null;
        }

        base.EjectBody(uid, cryopodSsdComponent);
        return contained;
    }

    private void HandleDragDropOn(EntityUid uid, CryopodSSDComponent cryopodSsdComponent, ref DragDropTargetEvent args)
    {
        if (cryopodSsdComponent.BodyContainer.ContainedEntity != null)
        {
            return;
        }
        
        if (!TryComp(args.Dragged, out MindComponent? mind) || !mind.HasMind)
        {
            _sawmill.Error($"{ToPrettyString(args.User)} tries to put non-playable entity into SSD cryopod {ToPrettyString(args.Dragged)}");
            return;
        }

        var doAfterArgs = new DoAfterArgs(args.User, cryopodSsdComponent.EntryDelay, new CryopodSSDDragFinished(), uid,
            target: args.Dragged, used: uid)
        {
            BreakOnDamage = true,
            BreakOnTargetMove = true,
            BreakOnUserMove = true,
            NeedHand = false,
        };
        _doAfterSystem.TryStartDoAfter(doAfterArgs);
        args.Handled = true;
    }

    private void OnDragFinished(EntityUid uid, CryopodSSDComponent component, CryopodSSDDragFinished args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target is null)
        {
            return;
        }

        if (InsertBody(uid, args.Args.Target.Value, component))
        {
            _sawmill.Info($"{ToPrettyString(args.Args.User)} put {ToPrettyString(args.Args.Target.Value)} inside cryopod.");
        }

        args.Handled = true;
    }
    
    private void OnCryopodSSDLeaveAction(EntityUid uid, CryopodSSDComponent component, CryopodSSDLeaveActionEvent args)
    {
        if (component.BodyContainer.ContainedEntity is null)
        {
            _sawmill.Error("This action cannot be called if no one is in the cryopod.");
            return;
        }
        TransferToCryoStorage(uid, component);
    }

    private void TransferToCryoStorage(EntityUid uid, CryopodSSDComponent? component)
    {
        if (!Resolve(uid, ref component) || component.BodyContainer.ContainedEntity is null)
        {
            return;
        }
        
        var ev = new TransferredToCryoStorageEvent(uid, component.BodyContainer.ContainedEntity.Value);

        ev.Handled = false;

        RaiseLocalEvent(uid, ev, true);
            
        if (!ev.Handled)
        {
            _SSDStorageConsoleSystem.TransferToCryoStorage(uid, component.BodyContainer.ContainedEntity.Value);
            ev.Handled = true;
        }
        UpdateAppearance(uid, component);
    }
}