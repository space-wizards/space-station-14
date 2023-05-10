using Content.Server.Forensics;
using Content.Shared.Body.Components;
using Content.Shared.Popups;
using Content.Shared.Timing;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Server.Changeling;

public sealed class ChangelingSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;

    public override void Initialize()
    {
        base.Initialize();

        // TODO: figure out abstract events
        SubscribeLocalEvent<ChangelingComponent, StingEvent>(OnExtractionSting);

        SubscribeLocalEvent<BodyComponent, GetVerbsEvent<AlternativeVerb>>(AddStingVerb);
    }

    // TODO: figure out abstract events
    private void OnExtractionSting(EntityUid uid, ChangelingComponent ling, ref StingEvent args)
    {
        // slime/vox have incompatible genomes so cant sting
        // target must also have all the required bits to transform
        // TODO: physical appearance
        if (!HasComp<AbsorbableComponent>(args.Target) || !TryComp<DnaComponent>(args.Target, out var dna) ||
            !TryComp<FingerprintComponent>(args.Target, out var prints) || prints.Fingerprint == null)
        {
            _popup.PopupEntity(Loc.GetString("changeling-extraction-incompatible"), uid, uid, PopupType.Medium);
            args.Cancel();
            return;
        }

        if (ling.ExtractedTransformations.Any(t => t.Dna == dna.DNA))
        {
            _popup.PopupEntity(Loc.GetString("changeling-extraction-already-extracted"), uid, uid, PopupType.Medium);
            args.Cancel();
            return;
        }

        // should probably never happen, at least without surgery
        if (ling.AbsorbedTransformations.Any(t => t.Dna == dna.DNA))
        {
            _popup.PopupEntity(Loc.GetString("changeling-extraction-already-absorbed"), uid, uid, PopupType.Medium);
            args.Cancel();
            return;
        }

        // TODO: physical appearance
        var name = MetaData(args.Target).EntityName;
        var transformation = new Transformation()
        {
            Name = name,
            Dna = dna.DNA,
            Fingerprint = prints.Fingerprint
        };

        // if too many transformations are stored, remove the oldest one
        if (ling.ExtractedTransformations.Count >= ling.MaxExtractedTransformations)
            ling.ExtractedTransformations.RemoveLast();

        ling.ExtractedTransformations.AddFirst(transformation);

        _popup.PopupEntity(Loc.GetString("changeling-extraction-success", ("target", name)), uid, uid, PopupType.Large);
        // TODO: extraction chemical boost
    }

    private void AddStingVerb(EntityUid uid, BodyComponent body, GetVerbsEvent<AlternativeVerb> args)
    {
        // no stinging yourself
        if (uid == args.User)
            return;

        // not a ling, no sting
        if (!TryComp<ChangelingComponent>(args.User, out var ling))
            return;

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("changeling-verb-sting-text"),
            Act = () => TrySting(args.User, ling, uid)
        });
    }

    private void TrySting(EntityUid uid, ChangelingComponent ling, EntityUid target)
    {
        // checked here and not when adding verb so lag doesn't prevent you from mass stinging
        TryComp<UseDelayComponent>(uid, out var useDelay);
        if (_useDelay.ActiveDelay(uid, useDelay))
            return;

        if (HasComp<ChangelingComponent>(target))
        {
            // alert both parties that the sting failed
            _popup.PopupEntity(Loc.GetString("changeling-burning-senstation"), target, target, PopupType.LargeCaution);
            _popup.PopupEntity(Loc.GetString("changeling-sting-failed"), uid, uid, PopupType.LargeCaution);
            return;
        }

        var sting = _proto.Index<StingPrototype>(ling.ActiveSting);
        // should be impossible but oh well
        if (sting.Event == null)
            return;

        if (sting.Cost > ling.Chemicals)
        {
            _popup.PopupEntity(Loc.GetString("changeling-sting-no-chemicals"), uid, uid, PopupType.Medium);
            return;
        }

        var ev = sting.Event;
        ev.Uncancel();
        ev.Target = target;

        Logger.Debug($"Raising event {ev}");
        RaiseLocalEvent(uid, ref ev);

        // only use chemicals and start cooldown if stinging succeeded
        if (!ev.Cancelled)
        {
            ling.Chemicals -= sting.Cost;
            useDelay = EnsureComp<UseDelayComponent>(uid);
            useDelay.Delay = sting.Delay;
            _useDelay.BeginDelay(uid, useDelay);
        }
    }
}
