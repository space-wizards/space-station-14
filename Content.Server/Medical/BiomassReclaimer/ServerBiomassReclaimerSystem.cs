using Content.Shared.Administration.Logs;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Events;
using Content.Shared.Medical.BiomassReclaimer;
using Content.Shared.Mind;
using Content.Shared.Popups;
using Robust.Server.Player;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;

namespace Content.Server.Medical.BiomassReclaimer;

public sealed partial class ServerBiomassReclaimerSystem : BiomassReclaimerSystem
{
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private IConfigurationManager _configManager = default!;
    [Dependency] private SharedMindSystem _minds = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private SharedAudioSystem _sharedAudioSystem = default!;

    private bool _biomassEasyMode;

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_configManager, CCVars.BiomassEasyMode, value => _biomassEasyMode = value, true);
    }

    [SubscribeLocalEvent]
    private void OnSuicideByEnvironment(Entity<BiomassReclaimerComponent> ent, ref SuicideByEnvironmentEvent args)
    {
        if (args.Handled || _activeQuery.HasComp(ent) || !_powerReceiver.IsPowered(ent.Owner))
            return;

        if (!_physicsQuery.TryComp(args.Victim, out var physics))
            return;

        _popup.PopupEntity(Loc.GetString("biomass-reclaimer-suicide-others", ("victim", Identity.Entity(args.Victim, EntityManager))),
            ent,
            PopupType.LargeCaution);
        StartProcessing((args.Victim, physics), ent);
        args.Handled = true;
    }

    protected override void StartRunningEffects(Entity<BiomassReclaimerComponent> ent)
    {
        base.StartRunningEffects(ent);
        _sharedAudioSystem.PlayPvs(ent.Comp.StartupSound, ent);
    }

    [SubscribeLocalEvent]
    private void OnDoAfter(Entity<BiomassReclaimerComponent> reclaimer, ref ReclaimerDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (args.Args.Used != reclaimer.Owner || args.Args.Target is not { } toProcess)
            return;

        if (!TryValidateInsertionAndPopup(reclaimer, toProcess, args.Args.User) || !_physicsQuery.TryComp(toProcess, out var physics))
            return;

        _adminLogger.Add(LogType.Action, LogImpact.High, $"{ToPrettyString(args.Args.User):player} used a biomass reclaimer to gib {ToPrettyString(toProcess):target} in {ToPrettyString(reclaimer):reclaimer}");
        StartProcessing((toProcess, physics), reclaimer);

        args.Handled = true;
    }

    protected override BiomassReclaimerInsertResult ValidateInsertion(Entity<BiomassReclaimerComponent> reclaimer, EntityUid dragged)
    {
        var result = base.ValidateInsertion(reclaimer, dragged);
        if (result != BiomassReclaimerInsertResult.Success)
            return result;

        // Reject souled bodies in easy mode.
        if (!_biomassEasyMode ||
            !HasComp<HumanoidProfileComponent>(dragged) ||
            !_minds.TryGetMind(dragged, out _, out var mind))
            return BiomassReclaimerInsertResult.Success;

        return mind.UserId == null || !_playerManager.TryGetSessionById(mind.UserId.Value, out _)
            ? BiomassReclaimerInsertResult.Success
            : BiomassReclaimerInsertResult.SoulPresent;
    }
}
