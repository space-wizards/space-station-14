using Content.Shared.Beam.Components;
using Content.Shared.DoAfter;
using Content.Shared.Foldable;
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
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly FoldableSystem _foldableSystem = default!;
    [Dependency] private readonly INetManager _net = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PosterComponent, AfterInteractEvent>(OnInteract);
        SubscribeLocalEvent<PosterComponent, PosterPlacingDoAfterEvent>(OnPlacingDoAfter);
        SubscribeLocalEvent<PosterComponent, PosterRemovingDoAfterEvent>(OnRemovingDoAfter);
        SubscribeLocalEvent<PosterComponent, GetVerbsEvent<UtilityVerb>>(OnPlacingVerb);
        SubscribeLocalEvent<PosterComponent, GetVerbsEvent<AlternativeVerb>>(OnRemoveVerb);
    }

    private void OnInteract(Entity<PosterComponent> poster, ref AfterInteractEvent args)
    {
        if (!args.CanReach || args.Target == null)
            return;

        if (CanPlacePoster(poster, args.Target.Value))
            StartPlacing(poster, args.Target.Value, args.User);
    }

    private void StartPlacing(Entity<PosterComponent> poster, EntityUid target, EntityUid user)
    {
        var comp = poster.Comp;

        var ev = new PosterPlacingDoAfterEvent();

        var doAfterargs = new DoAfterArgs(EntityManager, user, comp.PlacingTime, ev, poster, target, poster)
        {
            BreakOnMove = true,
            NeedHand = true,
            BreakOnHandChange = true
        };

        _audioSystem.PlayPredicted(comp.PlacingSound, poster, user);

        if (_doAfter.TryStartDoAfter(doAfterargs))
            CreatePlaceEffect(poster, target);
    }

    /// <summary>
    /// Creating poster place effect on server side
    /// </summary>
    private void CreatePlaceEffect(Entity<PosterComponent> poster, EntityUid target)
    {
        if (!_net.IsServer)
            return;

        var comp = poster.Comp;
        comp.EffectEntity = Spawn(comp.PlacingEffect, Transform(target).Coordinates);
    }

    private void DeletePlaceEffect(Entity<PosterComponent> poster)
    {
        if (!_net.IsServer)
            return;

        var comp = poster.Comp;

        QueueDel(comp.EffectEntity);
    }

    private void OnPlacingDoAfter(Entity<PosterComponent> poster, ref PosterPlacingDoAfterEvent args)
    {
        // Delete effect in case doafter was cancelled
        if (args.Cancelled || args.Handled)
        {
            DeletePlaceEffect(poster);
            return;
        }

        if (args.Target == null)
            return;

        // Getting foldable comp to unfold poster
        if (!TryComp<FoldableComponent>(poster, out var foldable))
            return;

        // Moving entity to the target coords
        var xform = Transform(poster);
        var parentXform = Transform(args.Target.Value);

        _transformSystem.SetCoordinates(poster, xform, parentXform.Coordinates, rotation: Angle.Zero);
        _transformSystem.SetParent(poster, args.Target.Value);

        _foldableSystem.SetFolded(poster, foldable, false);

        DeletePlaceEffect(poster);
    }

    /// <summary>
    /// Creates utility verb to place poster on the object
    /// </summary>
    private void OnPlacingVerb(Entity<PosterComponent> poster, ref GetVerbsEvent<UtilityVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        if (!CanPlacePoster(poster, args.Target))
            return;

        if (!_foldableSystem.IsFolded(poster))
            return;

        var target = args.Target;
        var user = args.User;

        var verb = new UtilityVerb()
        {
            Act = () => StartPlacing(poster, target, user),
            IconEntity = GetNetEntity(poster),
            Text = Loc.GetString("poster-placing-verb-text"),
            Message = Loc.GetString("poster-placing-verb-message")
        };

        args.Verbs.Add(verb);
    }

    private void OnRemoveVerb(Entity<PosterComponent> poster, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        // Check if poster is folded to prevent removing folded poster
        if (_foldableSystem.IsFolded(poster))
            return;

        var user = args.User;
        var verb = new AlternativeVerb()
        {
            Act = () => StartRemoving(poster, user),
            IconEntity = GetNetEntity(poster),
            Text = Loc.GetString("poster-removing-verb-text"),
            Message = Loc.GetString("poster-removing-verb-message")
        };

        args.Verbs.Add(verb);
    }

    private void StartRemoving(Entity<PosterComponent> poster, EntityUid user)
    {
        var comp = poster.Comp;

        var doAfterargs = new DoAfterArgs(EntityManager, user, comp.RemovingTime, new PosterRemovingDoAfterEvent(), poster, poster)
        {
            BreakOnMove = true,
            NeedHand = true,
            BreakOnHandChange = true
        };

        _audioSystem.PlayPredicted(comp.RemovingSound, poster, user);

        _doAfter.TryStartDoAfter(doAfterargs);
    }

    private void OnRemovingDoAfter(Entity<PosterComponent> poster, ref PosterRemovingDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (!TryComp<FoldableComponent>(poster, out var foldable))
            return;

        // Fold poster and put it in user's hands
        _foldableSystem.SetFolded(poster, foldable, true);
        _handsSystem.PickupOrDrop(args.User, poster, checkActionBlocker: false);
    }

    /// <summary>
    /// Check if poster can be placed on specific target
    /// </summary>
    private bool CanPlacePoster(Entity<PosterComponent> poster, EntityUid target)
    {
        var comp = poster.Comp;

        // Check if tag is empty. If it is - we starting placing poster on target object.
        if (string.IsNullOrWhiteSpace(comp.PlacingTag))
            return true;

        if (!HasComp<TagComponent>(target))
            return false;

        if (!_tagSystem.HasTag(target, comp.PlacingTag))
            return false;

        return true;
    }
}

[Serializable, NetSerializable]
public sealed partial class PosterPlacingDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed partial class PosterRemovingDoAfterEvent : SimpleDoAfterEvent
{
}
