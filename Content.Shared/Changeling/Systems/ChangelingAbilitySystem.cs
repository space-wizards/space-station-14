using System.Linq;
using Content.Shared.Actions;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Changeling.Components;
using Content.Shared.Cuffs;
using Content.Shared.Ensnaring;
using Content.Shared.Fluids;
using Content.Shared.IdentityManagement;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Robust.Shared.Audio.Systems;

namespace Content.Shared.Changeling.Systems;

public sealed partial class ChangelingAbilitySystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedCuffableSystem _cuffable = default!;
    [Dependency] private readonly SharedEnsnareableSystem _snare = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedPuddleSystem _puddle = default!;
    [Dependency] private readonly SharedChangelingIdentitySystem _changelingIdentity = default!;
    [Dependency] private readonly ChangelingDevourSystem _changelingDevour = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingBiodegradeAbilityComponent, ChangelingBiodegradeActionEvent>(OnBiodegradeAction);
        SubscribeLocalEvent<ChangelingIdentityComponent, ChangelingStingDnaEvent>(OnStingDna);
    }

    private void OnBiodegradeAction(Entity<ChangelingBiodegradeAbilityComponent> ent, ref ChangelingBiodegradeActionEvent args)
    {
        // Nothing can be done :(
        if (!_cuffable.IsCuffed(args.Performer) && !_snare.IsEnsnared(args.Performer))
            return;

        if (_pulling.GetPuller(args.Performer) is { } puller)
        {
            _stun.TryAddParalyzeDuration(puller, ent.Comp.PullerStunDuration);
        }

        var toDelete = new List<EntityUid>();

        _cuffable.TryGetAllCuffs(args.Performer, out var cuffs);
        foreach (var cuff in cuffs.ToList())
        {
            _cuffable.Uncuff(args.Performer, args.Performer, cuff);
            toDelete.Add(cuff);
        }

        toDelete.AddRange(_snare.ForceFreeAll(args.Performer));

        args.Handled = true;

        var selfPopup = Loc.TryGetString(ent.Comp.ActivatedPopupSelf, out var self, ("user", Identity.Entity(args.Performer, EntityManager)), ("restraint", toDelete.First())) ? self : null;
        var othersPopup = Loc.TryGetString(ent.Comp.ActivatedPopup, out var others, ("user", Identity.Entity(args.Performer, EntityManager)), ("restraint", toDelete.First())) ? others : null;

        _popup.PopupPredicted(selfPopup, othersPopup, args.Performer, args.Performer, PopupType.LargeCaution);
        _audio.PlayPredicted(ent.Comp.ActivatedSound, args.Performer, args.Performer);

        foreach (var deleted in toDelete)
        {
            PredictedQueueDel(deleted);
        }

        if (ent.Comp.SpillSolution != null)
            _puddle.TrySpillAt(args.Performer, ent.Comp.SpillSolution, out _, false);
    }

    private void OnStingDna(Entity<ChangelingIdentityComponent> ent, ref ChangelingStingDnaEvent args)
    {
        if (args.Target == ent.Owner)
            return; // Can't sting yourself.

        if (!_changelingDevour.CanDevour(ent.Owner, args.Target, checkDead: false, checkProtected: false))
            return;

        _popup.PopupClient(Loc.GetString("changeling-sting-success", ("target", Identity.Entity(args.Target, EntityManager))), args.Target, ent.Owner, PopupType.Medium);
        _changelingIdentity.GrantIdentity(ent, args.Target);

        args.Handled = true;
    }
}

/// <summary>
/// Action event for the Dna sting ability. Used to grand the changeling an identity without devouring somebody.
/// </summary>
public sealed partial class ChangelingStingDnaEvent : EntityTargetActionEvent;
