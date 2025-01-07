using Robust.Shared.Utility;
using Content.Server.Popups;
using Content.Server.Xenoarchaeology.Equipment.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts;
using Content.Shared.Interaction;
using Content.Shared.Timing;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;
using Content.Server.Paper;
using Robust.Server.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Content.Shared.Examine;
using Content.Shared.Plankton;
using System.Linq;

namespace Content.Server.Plankton;

public sealed class PlanktonScannerSystem : EntitySystem
{
    [Dependency] private readonly UseDelaySystem _useDelay = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly PaperSystem _paper = default!;
    [Dependency] private readonly MetaDataSystem _metaSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PlanktonScannerComponent, BeforeRangedInteractEvent>(OnBeforeRangedInteract);
        SubscribeLocalEvent<PlanktonScannerComponent, GetVerbsEvent<UtilityVerb>>(AddScanVerb);
        SubscribeLocalEvent<PlanktonScannerComponent, GetVerbsEvent<ActivationVerb>>(AddToggleAnalysisVerb);
        SubscribeLocalEvent<PlanktonScannerComponent, ExaminedEvent>(OnExamine);
    }

    private void OnBeforeRangedInteract(EntityUid uid, PlanktonScannerComponent component, BeforeRangedInteractEvent args)
{
    if (args.Handled || !args.CanReach || !args.Target.HasValue)
        return;

    var target = args.Target.Value; // Safe to use Value here
    if (!TryComp<PlanktonComponent>(target, out var plankton))
        return;

    CreatePopup(uid, target, plankton, component); // Now passing a non-null EntityUid

    args.Handled = true;
}


    private void AddScanVerb(EntityUid uid, PlanktonScannerComponent component, GetVerbsEvent<UtilityVerb> args)
    {
        if (!args.CanAccess)
            return;

        if (!TryComp<PlanktonComponent>(args.Target, out var plankton))
            return;

        var verb = new UtilityVerb()
        {
            Act = () =>
            {
                CreatePopup(uid, args.Target, plankton, component); // Fixed this line
            },
            Text = Loc.GetString("plankton-scan-tooltip")
        };

        args.Verbs.Add(verb);
    }

    private void AddToggleAnalysisVerb(EntityUid uid, PlanktonScannerComponent component, GetVerbsEvent<ActivationVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        ActivationVerb verb = new()
        {
            Text = Loc.GetString("toggle-analysis-verb-get-data-text"),
            Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/light.svg.192dpi.png")),
            Act = () => TryToggleAnalysis((uid, component), args.User),
            Priority = -1 // For things like PDA's, Open-UI, etc.
        };

        args.Verbs.Add(verb);
    }


    private void TryToggleAnalysis((EntityUid, PlanktonScannerComponent) data, EntityUid user)
{
    var (uid, component) = data;
    component.AnalysisMode = !component.AnalysisMode;
}


    private void CreatePopup(EntityUid uid, EntityUid target, PlanktonComponent component, PlanktonScannerComponent scanner)
    {
        if (TryComp(uid, out UseDelayComponent? useDelay)
            && !_useDelay.TryResetDelay((uid, useDelay), true))
            return;

        // Collects plankton species and living status from the target
        var planktonNames = component.SpeciesInstances
            .Select(species => $"{species.SpeciesName} - {(species.IsAlive ? "ALIVE" : "DEAD")}")
            .ToList();

        // Header for the paper report
        var message = Loc.GetString("plankton-scan-popup", ("count", $"{component.SpeciesInstances.Count}"));

        // Same as the paper report header, but just the popup.
        var messagePopup = Loc.GetString("plankton-scan-popup", ("count", $"{component.SpeciesInstances.Count}"));

        var rewardPopup = Loc.GetString("plankton-reward-popup");

        // Add the species names and status to the message if there are any
        if (planktonNames.Count > 0)
        {
            message += "\nSpecies names:\n" + string.Join("\n", planktonNames) + $"\nAmount of dead plankton: {component.DeadPlankton}";
        }

        if (planktonNames.Count == 1 && scanner.AnalysisMode)
        {
            var species = component.SpeciesInstances.First();
            if (species.CurrentSize >= 50)
            {
                if ((species.Characteristics & PlanktonComponent.PlanktonCharacteristics.HyperExoticSpecies) != 0)
                {
                    var rewardSuper = Spawn(scanner.PlanktonAdvancedRewardEntityId, Transform(uid).Coordinates);
                    _popupSystem.PopupEntity(rewardPopup, target);
                    _audioSystem.PlayPvs(scanner.PrintSound, uid);
                }
                else
                {
                    var reward = Spawn(scanner.PlanktonRewardEntityId, Transform(uid).Coordinates);
                    _popupSystem.PopupEntity(rewardPopup, target);
                    _audioSystem.PlayPvs(scanner.PrintSound, uid);
                }
            }
            else
            {
                if (species.CurrentSize < 50) _popupSystem.PopupEntity("plankton-too-small-alert", target);
            }
        }
        else
        {
            if (planktonNames.Count > 1) _popupSystem.PopupEntity("too-many-plankton-alert", target);
            if (planktonNames.Count == 0) _popupSystem.PopupEntity("no-plankton-alert", target);
        }

        if (!scanner.AnalysisMode)
        {
            _popupSystem.PopupEntity(messagePopup, target);

            var report = Spawn(scanner.PlanktonReportEntityId, Transform(uid).Coordinates);
            _metaSystem.SetEntityName(report, Loc.GetString("plankton-analysis-report-title", ("id", $"Plankton Scan Report")));
            _audioSystem.PlayPvs(scanner.PrintSound, uid);

            _paper.SetContent(report, message);
        }
    }

    private void OnExamine(EntityUid uid, PlanktonScannerComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var text = component.AnalysisMode
        ? "analysis-mode-on"
        : "analysis-mode-off";

        args.PushMarkup(Loc.GetString(text));
    }
}
