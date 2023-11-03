
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Robust.Shared.Utility;
using Content.Shared.NecroobeliskStoper;
using Content.Shared.Anomaly.Components;
using Content.Shared.DoAfter;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Shared.Popups;
using Content.Shared.Necroobelisk.Components;
using Content.Shared.Necroobelisk;

namespace Content.Server.Necroobelisk;

/// <summary>
/// This handles the anomaly scanner and it's UI updates.
/// </summary>
public sealed class NecroobeliskStoperSystem : EntitySystem
{

    //[Dependency] private readonly IConfigurationManager _configuration = default!;
    //[Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPointLightSystem _pointLight = default!;
    [Dependency] protected readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    public override void Initialize()
    {

        base.Initialize();

        SubscribeLocalEvent<NecroobeliskStoperComponent, AfterInteractEvent>(OnScannerAfterInteract); //AfterInteractEvent
        SubscribeLocalEvent<NecroobeliskStoperComponent, NecroobeliskStoperDoAfterEvent>(OnDoAfter);
    }


    private void OnScannerAfterInteract(EntityUid uid, NecroobeliskStoperComponent component, AfterInteractEvent args)
    {
        if (args.Target is not { } target)
            return;
        if (!HasComp<NecroobeliskComponent>(target))
            return;

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, component.ScanDoAfterDuration, new NecroobeliskStoperDoAfterEvent(), uid, target: target, used: uid)
        {
            DistanceThreshold = 2f
        });
    }

    private void OnDoAfter(EntityUid uid, NecroobeliskStoperComponent component, DoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target == null)
            return;

        if (!EntityManager.TryGetComponent<NecroobeliskComponent>(args.Args.Target.Value, out var xform))
            return;

        xform.Active -= 1;

        _audio.PlayPvs(component.CompleteSound, uid);

        //QueueDel(args.Args.Target.Value);
        QueueDel(uid);

        args.Handled = true;
    }

}
