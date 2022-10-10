using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Foldable;

[UsedImplicitly]
public abstract class SharedFoldableSystem : EntitySystem
{
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FoldableComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<FoldableComponent, ComponentHandleState>(OnHandleState);

        SubscribeLocalEvent<FoldableComponent, ComponentInit>(OnFoldableInit);
        SubscribeLocalEvent<FoldableComponent, ContainerGettingInsertedAttemptEvent>(OnInsertEvent);
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
            SetFolded(component, state.IsFolded);
    }

    private void OnFoldableInit(EntityUid uid, FoldableComponent component, ComponentInit args)
    {
        SetFolded(component, component.IsFolded);
    }

    /// <summary>
    /// Set the folded state of the given <see cref="FoldableComponent"/>
    /// </summary>
    /// <param name="component"></param>
    /// <param name="folded">If true, the component will become folded, else unfolded</param>
    public virtual void SetFolded(FoldableComponent component, bool folded)
    {
        component.IsFolded = folded;
        Dirty(component);
        Appearance.SetData(component.Owner, FoldedVisuals.State, folded);
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
