using Content.Shared.Access.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.Database;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Labels.EntitySystems;
using Content.Shared.Paper;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System.Text;

namespace Content.Server.CartridgeLoader.Cartridges;

public sealed class LogProbeCartridgeSystem : EntitySystem
{
    [Dependency] private readonly CartridgeLoaderSystem _cartridge = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedLabelSystem _label = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly PaperSystem _paper = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LogProbeCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
        SubscribeLocalEvent<LogProbeCartridgeComponent, CartridgeAfterInteractEvent>(AfterInteract);
        SubscribeLocalEvent<LogProbeCartridgeComponent, CartridgeMessageEvent>(OnMessage);
    }

    /// <summary>
    /// The <see cref="CartridgeAfterInteractEvent" /> gets relayed to this system if the cartridge loader is running
    /// the LogProbe program and someone clicks on something with it. <br/>
    /// <br/>
    /// Updates the program's list of logs with those from the device.
    /// </summary>
    private void AfterInteract(Entity<LogProbeCartridgeComponent> ent, ref CartridgeAfterInteractEvent args)
    {
        if (args.InteractEvent.Handled || !args.InteractEvent.CanReach || args.InteractEvent.Target is not { } target)
            return;

        if (!TryComp(target, out AccessReaderComponent? accessReaderComponent))
            return;

        //Play scanning sound with slightly randomized pitch
        _audio.PlayEntity(ent.Comp.SoundScan, args.InteractEvent.User, target);
        _popup.PopupCursor(Loc.GetString("log-probe-scan", ("device", target)), args.InteractEvent.User);

        ent.Comp.EntityName = Name(target);
        ent.Comp.PulledAccessLogs.Clear();

        foreach (var accessRecord in accessReaderComponent.AccessLog)
        {
            var log = new PulledAccessLog(
                accessRecord.AccessTime,
                accessRecord.Accessor
            );

            ent.Comp.PulledAccessLogs.Add(log);
        }

        // Reverse the list so the oldest is at the bottom
        ent.Comp.PulledAccessLogs.Reverse();

        UpdateUiState(ent, args.Loader);
    }

    /// <summary>
    /// This gets called when the ui fragment needs to be updated for the first time after activating
    /// </summary>
    private void OnUiReady(Entity<LogProbeCartridgeComponent> ent, ref CartridgeUiReadyEvent args)
    {
        UpdateUiState(ent, args.Loader);
    }

    private void OnMessage(Entity<LogProbeCartridgeComponent> ent, ref CartridgeMessageEvent args)
    {
        if (args is LogProbePrintMessage cast)
            PrintLogs(ent, cast.User);
    }

    private void PrintLogs(Entity<LogProbeCartridgeComponent> ent, EntityUid user)
    {
        if (string.IsNullOrEmpty(ent.Comp.EntityName))
            return;

        if (_timing.CurTime < ent.Comp.NextPrintAllowed)
            return;

        ent.Comp.NextPrintAllowed = _timing.CurTime + ent.Comp.PrintCooldown;

        var paper = Spawn(ent.Comp.PaperPrototype, _transform.GetMapCoordinates(user));
        _label.Label(paper, ent.Comp.EntityName); // label it for easy identification

        _audio.PlayEntity(ent.Comp.PrintSound, user, paper);
        _hands.PickupOrDrop(user, paper, checkActionBlocker: false);

        // generate the actual printout text
        var builder = new StringBuilder();
        builder.AppendLine(Loc.GetString("log-probe-printout-device", ("name", ent.Comp.EntityName)));
        builder.AppendLine(Loc.GetString("log-probe-printout-header"));
        var number = 1;
        foreach (var log in ent.Comp.PulledAccessLogs)
        {
            var time = TimeSpan.FromSeconds(Math.Truncate(log.Time.TotalSeconds)).ToString();
            builder.AppendLine(Loc.GetString("log-probe-printout-entry", ("number", number), ("time", time), ("accessor", log.Accessor)));
            number++;
        }

        var paperComp = Comp<PaperComponent>(paper);
        _paper.SetContent((paper, paperComp), builder.ToString());

        _adminLogger.Add(LogType.EntitySpawn, LogImpact.Low, $"{ToPrettyString(user):user} printed out LogProbe logs ({paper}) of {ent.Comp.EntityName}");
    }

    private void UpdateUiState(Entity<LogProbeCartridgeComponent> ent, EntityUid loaderUid)
    {
        var state = new LogProbeUiState(ent.Comp.EntityName, ent.Comp.PulledAccessLogs);
        _cartridge.UpdateCartridgeUiState(loaderUid, state);
    }
}
