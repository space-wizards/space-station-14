using System.Linq;
using Content.Shared.Changeling.Components;
using Content.Shared.Cuffs;
using Content.Shared.Ensnaring;
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

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingBiodegradeAbilityComponent, ChangelingBiodegradeActionEvent>(OnBiodegradeAction);
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

        List<EntityUid> toDelete = new List<EntityUid>();

        _cuffable.TryGetAllCuffs(args.Performer, out var cuffs);
        foreach (var cuff in cuffs.ToList())
        {
            _cuffable.Uncuff(args.Performer, args.Performer, cuff);
            toDelete.Add(cuff);
        }

        toDelete.AddRange(_snare.ForceFreeAll(args.Performer));

        args.Handled = true;

        // How can you be ensnared/cuffed and have nothing detected??
        if (toDelete.Count == 0)
            return;

        var selfPopup = Loc.TryGetString(ent.Comp.ActivatedPopupSelf, out var self, ("user", Identity.Entity(args.Performer, EntityManager)), ("cuffs", toDelete.First())) ? self : null;
        var othersPopup = Loc.TryGetString(ent.Comp.ActivatedPopup, out var others, ("user", Identity.Entity(args.Performer, EntityManager)), ("cuffs", toDelete.First())) ? others : null;

        _popup.PopupPredicted(othersPopup, selfPopup, args.Performer, args.Performer, PopupType.LargeCaution);
        _audio.PlayPredicted(ent.Comp.ActivatedSound, args.Performer, args.Performer);

        foreach (var deleted in toDelete)
        {
            PredictedQueueDel(deleted);
        }

        // TODO: Should probably spawn a puddle of acid. But solutions are frozen due to an upcoming refactor.
    }
}
