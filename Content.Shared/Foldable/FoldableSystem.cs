using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Storage.Components;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Foldable;

public sealed class FoldableSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedBuckleSystem _buckle = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FoldableComponent, GetVerbsEvent<AlternativeVerb>>(AddFoldVerb);
        SubscribeLocalEvent<FoldableComponent, AfterAutoHandleStateEvent>(OnHandleState);

        SubscribeLocalEvent<FoldableComponent, ComponentInit>(OnFoldableInit);
        SubscribeLocalEvent<FoldableComponent, ContainerGettingInsertedAttemptEvent>(OnInsertEvent);
        SubscribeLocalEvent<FoldableComponent, StoreMobInItemContainerAttemptEvent>(OnStoreThisAttempt);
        SubscribeLocalEvent<FoldableComponent, StorageOpenAttemptEvent>(OnFoldableOpenAttempt);

        SubscribeLocalEvent<FoldableComponent, BuckleAttemptEvent>(OnBuckleAttempt);
    }

    private void OnHandleState(EntityUid uid, FoldableComponent component, ref AfterAutoHandleStateEvent args)
    {
        SetFolded(uid, component, component.IsFolded);
    }

    private void OnFoldableInit(EntityUid uid, FoldableComponent component, ComponentInit args)
    {
        SetFolded(uid, component, component.IsFolded);
    }

    private void OnFoldableOpenAttempt(EntityUid uid, FoldableComponent component, ref StorageOpenAttemptEvent args)
    {
        if (component.IsFolded)
            args.Cancelled = true;
    }

    public void OnStoreThisAttempt(EntityUid uid, FoldableComponent comp, ref StoreMobInItemContainerAttemptEvent args)
    {
        args.Handled = true;

        if (comp.IsFolded)
            args.Cancelled = true;
    }

    public void OnBuckleAttempt(EntityUid uid, FoldableComponent comp, ref BuckleAttemptEvent args)
    {
        if (args.Buckling && comp.IsFolded)
            args.Cancelled = true;
    }

    /// <summary>
    /// Returns false if the entity isn't foldable.
    /// </summary>
    public bool IsFolded(EntityUid uid, FoldableComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        return component.IsFolded;
    }

    /// <summary>
    /// Set the folded state of the given <see cref="FoldableComponent"/>
    /// </summary>
    public void SetFolded(EntityUid uid, FoldableComponent component, bool folded)
    {
        component.IsFolded = folded;
        Dirty(uid, component);
        _appearance.SetData(uid, FoldedVisuals.State, folded);
        _buckle.StrapSetEnabled(uid, !component.IsFolded);

        var ev = new FoldedEvent(folded);
        RaiseLocalEvent(uid, ref ev);
    }

    private void OnInsertEvent(EntityUid uid, FoldableComponent component, ContainerGettingInsertedAttemptEvent args)
    {
        if (!component.IsFolded && !component.CanFoldInsideContainer)
            args.Cancel();
    }

    public bool TryToggleFold(EntityUid uid, FoldableComponent comp)
    {
        return TrySetFolded(uid, comp, !comp.IsFolded);
    }

    public bool CanToggleFold(EntityUid uid, FoldableComponent? fold = null)
    {
        if (!Resolve(uid, ref fold))
            return false;

        // Can't un-fold in any container unless enabled (locker, hands, inventory, whatever).
        if (_container.IsEntityInContainer(uid) && !fold.CanFoldInsideContainer)
            return false;

        var ev = new FoldAttemptEvent();
        RaiseLocalEvent(uid, ref ev);
        return !ev.Cancelled;
    }

    /// <summary>
    /// Try to fold/unfold
    /// </summary>
    public bool TrySetFolded(EntityUid uid, FoldableComponent comp, bool state)
    {
        if (state == comp.IsFolded)
            return false;

        if (!CanToggleFold(uid, comp))
            return false;

        SetFolded(uid, comp, state);
        return true;
    }

    #region Verb

    private void AddFoldVerb(EntityUid uid, FoldableComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null || !CanToggleFold(uid, component))
            return;

        AlternativeVerb verb = new()
        {
            Act = () => TryToggleFold(uid, component),
            Text = component.IsFolded ? Loc.GetString(component.UnfoldVerbText) : Loc.GetString(component.FoldVerbText),
            Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/fold.svg.192dpi.png")),

            // If the object is unfolded and they click it, they want to fold it, if it's folded, they want to pick it up
            Priority = component.IsFolded ? 0 : 2,
        };

        args.Verbs.Add(verb);
    }

    #endregion

    [Serializable, NetSerializable]
    public enum FoldedVisuals : byte
    {
        State
    }
}

/// <summary>
/// Event raised on an entity to determine if it can be folded.
/// </summary>
/// <param name="Cancelled"></param>
[ByRefEvent]
public record struct FoldAttemptEvent(bool Cancelled = false);

/// <summary>
/// Event raised on an entity after it has been folded.
/// </summary>
/// <param name="IsFolded"></param>
[ByRefEvent]
public readonly record struct FoldedEvent(bool IsFolded);
