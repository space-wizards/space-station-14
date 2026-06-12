using Content.Shared._Offbrand.Skeletons;
using Content.Shared._Offbrand.Wounds;
using Content.Shared.Body;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Localizations;
using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;

namespace Content.Shared._Offbrand.Medical;

public sealed partial class PalpatableOrganSystem : EntitySystem
{
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;
    [Dependency] private EntityQuery<ParentOrganComponent> _parentOrganQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StatusEffectContainerComponent, PalpationEvent>(_statusEffects.RelayEvent);

        SubscribeLocalEvent<PalpationDescriptionComponent, PalpationEvent>(OnPalpation);
        SubscribeLocalEvent<PalpationDescriptionComponent, StatusEffectRelayedEvent<PalpationEvent>>(OnRelayedPalpation);

        SubscribeLocalEvent<PalpatableOrganComponent, ActivateInWorldEvent>(OnActivateInWorld);
        SubscribeLocalEvent<PalpatableOrganComponent, PalpationDoAfterEvent>(OnDoAfter);
    }

    private void OnActivateInWorld(Entity<PalpatableOrganComponent> ent, ref ActivateInWorldEvent args)
    {
        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, ent.Comp.Delay, new PalpationDoAfterEvent(), ent, target: ent, used: ent)
        {
            DuplicateCondition = DuplicateConditions.SameEvent | DuplicateConditions.SameTarget,
            BreakOnMove = true,
            BreakOnHandChange = false,
        });
    }

    private void OnPalpation(Entity<PalpationDescriptionComponent> ent, ref PalpationEvent args)
    {
        AddDescription(ent, ent, ref args);
    }

    private void OnRelayedPalpation(Entity<PalpationDescriptionComponent> ent,
        ref StatusEffectRelayedEvent<PalpationEvent> args)
    {
        if (Comp<StatusEffectComponent>(ent).AppliedTo is not { } appliedTo)
            return;

        var ev = args.Args;
        AddDescription(ent, appliedTo, ref ev);
        args.Args = ev;
    }

    private void AddDescription(Entity<PalpationDescriptionComponent> ent, EntityUid organ, ref PalpationEvent args)
    {
        args.Messages.Add(Loc.GetString(ent.Comp.Description, ("organ", organ)));
    }

    private void OnDoAfter(Entity<PalpatableOrganComponent> ent, ref PalpationDoAfterEvent args)
    {
        if (args.Handled || args.Target is null || args.Cancelled)
            return;

        var ev = new PalpationEvent(new());

        RaiseLocalEvent(ent, ref ev);
        CheckPulse(ent, ref ev); // not a subscription to avoid child organs double-reporting pulse

        if (_parentOrganQuery.TryGetComponent(ent, out var parent))
        {
            foreach (var child in parent.Children)
            {
                if (Exists(child) && HasComp<InternalChildOrganComponent>(child))
                    RaiseLocalEvent(child, ref ev);
            }
        }

        if (ev.Messages.Count == 0)
            _popup.PopupPredicted(Loc.GetString("palpation-nothing"), ent, args.User);
        else
            _popup.PopupPredicted(Loc.GetString("palpation-feels", ("feels", ContentLocalizationManager.FormatList(ev.Messages))), ent, args.User);
    }

    private void CheckPulse(Entity<PalpatableOrganComponent> ent, ref PalpationEvent args)
    {
        if (Comp<OrganComponent>(ent).Body is not { } body)
            return;

        if (!TryComp<PerfusionComponent>(body, out var perfusion))
            return;

        if (ent.Comp.PulseQualities.HighestMatch(perfusion.Perfusion) is not { } quality)
            return;

        if (ent.Comp.PulseSpeeds.HighestMatch(perfusion.Strain) is not { } speeds)
            return;

        args.Messages.Add(Loc.GetString(speeds, ("quality", quality)));
    }
}
