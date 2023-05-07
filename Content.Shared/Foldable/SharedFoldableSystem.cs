using Content.Shared.Buckle;
using Content.Shared.Storage.Components;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Foldable;

[UsedImplicitly]
public abstract class SharedFoldableSystem : EntitySystem
{
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;
    [Dependency] private readonly SharedBuckleSystem _buckle = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FoldableComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<FoldableComponent, ComponentHandleState>(OnHandleState);

        SubscribeLocalEvent<FoldableComponent, ComponentInit>(OnFoldableInit);
        SubscribeLocalEvent<FoldableComponent, ContainerGettingInsertedAttemptEvent>(OnInsertEvent);
        SubscribeLocalEvent<FoldableComponent, StoreMobInItemContainerAttemptEvent>(OnStoreThisAttempt);
        SubscribeLocalEvent<FoldableComponent, StorageOpenAttemptEvent>(OnFoldableOpenAttempt);
    }

    private void OnGetState(EntityUid uid, FoldableComponent component, ref ComponentGetState args)
    {
        args.State = new FoldableComponentState(component.IsFolded);
    }

    private void OnHandleState(EntityUid uid, FoldableComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not FoldableComponentState state)
            return;

        if (state.IsFolded != component.IsFolded)
            SetFolded(uid, component, state.IsFolded);
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

    /// <summary>
    /// Set the folded state of the given <see cref="FoldableComponent"/>
    /// </summary>
    public virtual void SetFolded(EntityUid uid, FoldableComponent component, bool folded)
    {
        component.IsFolded = folded;
        Dirty(component);
        Appearance.SetData(uid, FoldedVisuals.State, folded);
        _buckle.StrapSetEnabled(uid, !component.IsFolded);
    }

    private void OnInsertEvent(EntityUid uid, FoldableComponent component, ContainerGettingInsertedAttemptEvent args)
    {
        if (!component.IsFolded)
            args.Cancel();
    }

    [Serializable, NetSerializable]
    public enum FoldedVisuals : byte
    {
        State
    }
}
