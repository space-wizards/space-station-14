using Content.Server.Popups;
using Content.Server.Xenoarchaeology.Equipment.Components;
using Content.Server.Xenoarchaeology.XenoArtifacts;
using Content.Shared.Interaction;
using Content.Shared.Timing;
using Content.Shared.Verbs;

namespace Content.Server.Plankton;

public sealed class PlankonScannerSystem : EntitySystem
{
    [Dependency] private readonly UseDelaySystem _useDelay = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PlanktonScannerComponent, BeforeRangedInteractEvent>(OnBeforeRangedInteract);
        SubscribeLocalEvent<PlanktonScannerComponent, GetVerbsEvent<UtilityVerb>>(AddScanVerb);
    }

    private void OnBeforeRangedInteract(EntityUid uid, PlanktonScannerComponent component, BeforeRangedInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not {} target)
            return;

        if (!TryComp<PlanktonComponent>(target, out var plankton))
            return;

        CreatePopup(uid, target, plankton);
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
                CreatePopup(uid, args.Target, plankton);
            },
            Text = Loc.GetString("plankton-scan-tooltip")
        };

        args.Verbs.Add(verb);
    }

    private void CreatePopup(EntityUid uid, EntityUid target, PlanktonComponent component, PlanktonScannerComponent scanner)
{
    if (TryComp(uid, out UseDelayComponent? useDelay)
        && !_useDelay.TryResetDelay((uid, useDelay), true))
        return;

    // Collect plankton species names
    var planktonNames = component.SpeciesInstances
        .Select(species => species.SpeciesName.ToString())  // Use the ToString method of PlanktonName
        .ToList();

    // Prepare the popup message
    var message = Loc.GetString("plankton-scan-popup",
        ("count", $"{component.SpeciesInstances.Count}"));

    // Add the species names to the message if there are any
    if (planktonNames.Count > 0)
    {
        message += "\nSpecies names:\n" + string.Join("\n", planktonNames);
    }

    _popupSystem.PopupEntity(message, target);

    var report = Spawn(scanner.PlanktonReportEntityId, Transform(uid).Coordinates);
    _metaSystem.SetEntityName(report, Loc.GetString("plankton-analysis-report-title", ("id", $"Plankton Scan Report")));

    _paper.SetContent(report, message.ToMarkup());
}

}
