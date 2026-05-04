using Content.Shared._Offbrand.UserInterface;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared._Offbrand.Analyzers;

public sealed class HandheldAnalyzerSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly AnalyzerSystem _analyzer = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _userInterface = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HandheldAnalyzerComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<HandheldAnalyzerComponent, AfterAnalyzerUpdatedEvent>(OnAfterUpdated);
        SubscribeLocalEvent<HandheldAnalyzerComponent, HandheldAnalyzerDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<HandheldAnalyzerComponent, OpenBoundInterfaceMessage>(OnOpenBui);
        SubscribeLocalEvent<HandheldAnalyzerComponent, CloseBoundInterfaceMessage>(OnCloseBui);
    }

    private void OnAfterUpdated(Entity<HandheldAnalyzerComponent> ent, ref AfterAnalyzerUpdatedEvent args)
    {
        _userInterface.SetUiState(ent.Owner, ent.Comp.UiKey, new DummyBoundUserInterfaceState());
    }

    private void OnAfterInteract(Entity<HandheldAnalyzerComponent> analyzer, ref AfterInteractEvent args)
    {
        if (!args.CanReach)
            return;

        if (!_entityWhitelist.CheckBoth(args.Target, analyzer.Comp.Blacklist, analyzer.Comp.Whitelist))
            return;

        _audio.PlayPredicted(analyzer.Comp.StartScanSound, analyzer, args.User);

        var started = _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
            args.User,
            analyzer.Comp.ScanDelay,
            new HandheldAnalyzerDoAfterEvent(),
            analyzer,
            target: args.Target,
            used: analyzer)
        {
            NeedHand = true,
            BreakOnMove = true,
        });

        if (args.Target == args.User || !started || analyzer.Comp.Silent)
            return;

        var msg = Loc.GetString("health-analyzer-popup-scan-target",
            ("user", Identity.Entity(args.User, EntityManager)));
        _popup.PopupEntity(msg, args.Target.Value, args.Target.Value, PopupType.Medium);
    }

    private void OnDoAfter(Entity<HandheldAnalyzerComponent> analyzer, ref HandheldAnalyzerDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target == null)
            return;

        if (!analyzer.Comp.Silent)
            _audio.PlayPredicted(analyzer.Comp.EndScanSound, analyzer, args.User);

        _analyzer.Analyze(analyzer.Owner, args.Target);
        _userInterface.OpenUi(analyzer.Owner, analyzer.Comp.UiKey, args.User, true);
    }

    private void OnOpenBui(Entity<HandheldAnalyzerComponent> analyzer, ref OpenBoundInterfaceMessage args)
    {
        if (!args.UiKey.Equals(analyzer.Comp.UiKey))
            return;

        _analyzer.SetShouldUpdate(analyzer.Owner, true);
    }

    private void OnCloseBui(Entity<HandheldAnalyzerComponent> analyzer, ref CloseBoundInterfaceMessage args)
    {
        if (!args.UiKey.Equals(analyzer.Comp.UiKey))
            return;

        _analyzer.SetShouldUpdate(analyzer.Owner, false);
    }
}

[Serializable, NetSerializable]
public sealed partial class HandheldAnalyzerDoAfterEvent : SimpleDoAfterEvent;
