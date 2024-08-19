using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Posters.Components;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Posters;

public abstract partial class SharedPosterSystem : EntitySystem
{
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly INetManager _net = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FoldedPosterComponent, AfterInteractEvent>(OnFoldedInteract);
        SubscribeLocalEvent<FoldedPosterComponent, PosterPlacingDoAfterEvent>(OnPlacingDoAfter);
        SubscribeLocalEvent<FoldedPosterComponent, GetVerbsEvent<UtilityVerb>>(OnPlacingVerb);

        SubscribeLocalEvent<PosterComponent, GetVerbsEvent<AlternativeVerb>>(OnRemoveVerb);
        SubscribeLocalEvent<PosterComponent, PosterRemovingDoAfterEvent>(OnRemovingDoAfter);
    }

    private void OnFoldedInteract(EntityUid uid, FoldedPosterComponent folded, AfterInteractEvent args)
    {
        if (folded.CancelToken != null || !args.CanReach || args.Target == null)
            return;

        if (!HasComp<TagComponent>(args.Target))
            return;

        if (folded.PosterPrototype == null)
            return;

        if (!_prototypeManager.HasIndex<EntityPrototype>(folded.PosterPrototype))
            return;

        if (_tagSystem.HasTag(args.Target.Value, "Wall"))
            StartPlacing(uid, folded, args.Target.Value, args.User);
    }

    private void OnPlacingDoAfter(EntityUid uid, FoldedPosterComponent folded, PosterPlacingDoAfterEvent args)
    {
        if (args.Cancelled || _net.IsServer)
            QueueDel(EntityManager.GetEntity(args.Effect));

        if (args.Handled || args.Cancelled)
            return;

        if (args.Target == null)
            return;

        Spawn(folded.PosterPrototype, Transform(args.Target.Value).Coordinates);
        QueueDel(uid);
        QueueDel(EntityManager.GetEntity(args.Effect));
    }

    private void OnRemovingDoAfter(EntityUid uid, PosterComponent poster, PosterRemovingDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        var item = Spawn(poster.FoldedPrototype, Transform(args.User).Coordinates);
        _handsSystem.PickupOrDrop(args.User, item, checkActionBlocker: false);

        QueueDel(uid);
    }

    private void OnPlacingVerb(EntityUid uid, FoldedPosterComponent folded, GetVerbsEvent<UtilityVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess || folded.CancelToken != null)
            return;

        if (!HasComp<TagComponent>(args.Target) || !_tagSystem.HasTag(args.Target, "Wall"))
            return;

        if (folded.PosterPrototype == null)
            return;

        if (!_prototypeManager.HasIndex<EntityPrototype>(folded.PosterPrototype))
            return;

        var verb = new UtilityVerb()
        {
            Act = () => StartPlacing(uid, folded, args.Target, args.User),
            IconEntity = GetNetEntity(uid),
            Text = Loc.GetString("poster-placing-verb-text"),
            Message = Loc.GetString("poster-placing-verb-message")
        };

        args.Verbs.Add(verb);
    }

    private void OnRemoveVerb(EntityUid uid, PosterComponent poster, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        if (poster.FoldedPrototype == null)
            return;

        if (!_prototypeManager.HasIndex<EntityPrototype>(poster.FoldedPrototype))
            return;

        var verb = new AlternativeVerb()
        {
            Act = () => StartRemoving(uid, poster, args.User),
            IconEntity = GetNetEntity(uid),
            Text = Loc.GetString("poster-removing-verb-text"),
            Message = Loc.GetString("poster-removing-verb-message")
        };

        args.Verbs.Add(verb);
    }

    private void StartPlacing(EntityUid uid, FoldedPosterComponent folded, EntityUid target, EntityUid user)
    {
        var effect = Spawn(folded.PlacingEffect, Transform(target).Coordinates);
        var ev = new PosterPlacingDoAfterEvent(EntityManager.GetNetEntity(effect));

        var doAfterargs = new DoAfterArgs(EntityManager, user, folded.PlacingTime, ev, uid, target, uid)
        {
            BreakOnMove = true,
            NeedHand = true,
            BreakOnHandChange = true
        };

        _audioSystem.PlayPvs(folded.PlacingSound, uid);

        if (!_doAfter.TryStartDoAfter(doAfterargs))
            QueueDel(effect);
    }

    private void StartRemoving(EntityUid uid, PosterComponent poster, EntityUid user)
    {
        var doAfterargs = new DoAfterArgs(EntityManager, user, poster.RemovingTime, new PosterRemovingDoAfterEvent(), uid, uid)
        {
            BreakOnMove = true,
            NeedHand = true,
            BreakOnHandChange = true
        };

        _audioSystem.PlayPvs(poster.RemovingSound, uid);

        _doAfter.TryStartDoAfter(doAfterargs);
    }
}

[Serializable, NetSerializable]
public sealed partial class PosterPlacingDoAfterEvent : DoAfterEvent
{
    public NetEntity? Effect { get; private set; } = null;

    public PosterPlacingDoAfterEvent(NetEntity? effect = null)
    {
        Effect = effect;
    }

    public override DoAfterEvent Clone() => this;
}

[Serializable, NetSerializable]
public sealed partial class PosterRemovingDoAfterEvent : SimpleDoAfterEvent
{
}
