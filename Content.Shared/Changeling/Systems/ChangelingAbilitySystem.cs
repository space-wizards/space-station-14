using Content.Shared.Changeling.Components;
using Content.Shared.Cuffs;
using Content.Shared.Cuffs.Components;
using Content.Shared.Ensnaring;
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
        if (!TryComp<CuffableComponent>(args.Performer, out var cuffs) || !_cuffable.TryGetLastCuff(args.Performer, out var cuff))
            return;

        if (_pulling.GetPuller(args.Performer) is { } puller)
        {
            _stun.TryAddParalyzeDuration(puller, ent.Comp.PullerStunDuration);
        }

        _audio.PlayPredicted(ent.Comp.ActivatedSound, args.Performer, args.Performer);


        var selfPopup = Loc.TryGetString(ent.Comp.ActivatedPopupSelf, out var self, ("user", args.Performer), ("cuffs", cuff)) ? self : null;
        var othersPopup = Loc.TryGetString(ent.Comp.ActivatedPopup, out var others, ("user", args.Performer), ("cuffs", cuff)) ? others : null;

        _popup.PopupPredicted(othersPopup, selfPopup, args.Performer, args.Performer, PopupType.LargeCaution);

        _cuffable.Uncuff(args.Performer, args.Performer, cuff.Value);
        _snare.ForceFreeAll(args.Performer);

        // TODO: Should probably spawn a puddle of acid. But solutions are frozen due to an upcoming refactor.

        args.Handled = true;
    }
}
