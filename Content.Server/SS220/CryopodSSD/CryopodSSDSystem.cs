// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Mind.Components;
using Content.Server.Station.Systems;
using Content.Shared.Audio;
using Content.Shared.CCVar;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Mobs.Systems;
using Content.Shared.SS220.CryopodSSD;
using Content.Shared.Verbs;
using Robust.Shared.Configuration;
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
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    private ISawmill _sawmill = default!;

    private float _autoTransferDelay;

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = Logger.GetSawmill("cryopodSSD");
        
        _cfg.OnValueChanged(CCVars.AutoTransferToCryoDelay, SetAutoTransferDelay, true);

        SubscribeLocalEvent<CryopodSSDComponent, ComponentInit>(OnComponentInit);

        SubscribeLocalEvent<CryopodSSDComponent, GetVerbsEvent<AlternativeVerb>>(AddAlternativeVerbs);
        SubscribeLocalEvent<CryopodSSDComponent, CryopodSSDLeaveActionEvent>(OnCryopodSSDLeaveAction);
        
        SubscribeLocalEvent<CryopodSSDComponent, TeleportToCryoFinished>(OnTeleportFinished);
        SubscribeLocalEvent<CryopodSSDComponent, CryopodSSDDragFinished>(OnDragFinished);
        SubscribeLocalEvent<CryopodSSDComponent, DragDropTargetEvent>(HandleDragDropOn);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var timeToAutoTransfer = _gameTiming.CurTime - TimeSpan.FromSeconds(_autoTransferDelay);

        var entityEnumerator = EntityQueryEnumerator<CryopodSSDComponent>();

        while (entityEnumerator.MoveNext(out var uid, out var cryopodSSDComp))
        {
            if (cryopodSSDComp.BodyContainer.ContainedEntity is null ||
                timeToAutoTransfer < cryopodSSDComp.EntityLiedInCryopodTime)
            {
                continue;
            }

            TransferToCryoStorage(uid, cryopodSSDComp);           
        }
    }
    
    public override void Shutdown()
    {
        base.Shutdown();
        
        _cfg.UnsubValueChanged(CCVars.AutoTransferToCryoDelay, SetAutoTransferDelay);
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
    
    
    /// <summary>
    /// Tries to teleport target inside cryopod, if any available
    /// </summary>
    /// <param name="target"> Target to teleport in first matching cryopod</param>
    /// <returns> true if player successfully transferred to cryo storage, otherwise returns false</returns>
    public bool TeleportEntityToCryoStorageWithDelay(EntityUid target)
    {
        var station = _stationSystem.GetOwningStation(target);

        if (station is null)
        {
            return false;
        }

        foreach (var comp in EntityQuery<CryopodSSDComponent>())
        {
            if (comp.BodyContainer.ContainedEntity == target)
            {
                return true;
            }
        }
        
        var cryopodSSDComponents = EntityQueryEnumerator<CryopodSSDComponent>();

        while (cryopodSSDComponents
               .MoveNext(out var cryopodSSDUid, out var cryopodSSDComp))
        {
            if (cryopodSSDComp.BodyContainer.ContainedEntity is null
                && _stationSystem.GetOwningStation(cryopodSSDUid) == station)
            {
                var portal = Spawn("CryoStoragePortal", Transform(target).Coordinates);
                
                if (TryComp<AmbientSoundComponent>(portal, out var ambientSoundComponent))
                {
                    _audioSystem.PlayPvs(ambientSoundComponent.Sound, portal);
                }
                
                var doAfterArgs = new DoAfterArgs(target, cryopodSSDComp.EntryDelay, new TeleportToCryoFinished(portal), cryopodSSDUid)
                {
                    BreakOnDamage = false,
                    BreakOnTargetMove = false,
                    BreakOnUserMove = true,
                    NeedHand = false,
                };

                _doAfterSystem.TryStartDoAfter(doAfterArgs);
                return true;
            }
        }

        return false;
    }

    private void OnTeleportFinished(EntityUid uid, CryopodSSDComponent component, TeleportToCryoFinished args)
    {
        InsertBody(uid, args.User, component);
        TransferToCryoStorage(uid, component);

        if (TryComp<AmbientSoundComponent>(args.PortalId, out var ambientSoundComponent))
        {
            _audioSystem.PlayPvs(ambientSoundComponent.Sound, args.PortalId);
        }

        EntityManager.DeleteEntity(args.PortalId);
    }

    private void SetAutoTransferDelay(float value) => _autoTransferDelay = value;
    
    private void HandleDragDropOn(EntityUid uid, CryopodSSDComponent cryopodSsdComponent, ref DragDropTargetEvent args)
    {
        if (cryopodSsdComponent.BodyContainer.ContainedEntity != null)
        {
            return;
        }
        
        if (!TryComp(args.Dragged, out MindComponent? mind) || !mind.HasMind)
        {
            _sawmill.Info($"{ToPrettyString(args.User)} tries to put non-playable entity into SSD cryopod {ToPrettyString(args.Dragged)}");
            return;
        }

        if (_mobStateSystem.IsDead(args.Dragged))
        {
            _sawmill.Log(LogLevel.Warning,$"{ToPrettyString(args.User)} tries to put dead entity(passing client check) {ToPrettyString(args.Dragged)} into SSD cryopod, potentially client exploit");
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