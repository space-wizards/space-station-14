using Content.Shared.Item;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Shared.Foldable;

[UsedImplicitly]
public abstract class SharedFoldableSystem : EntitySystem
{
    private const string FoldKey = "FoldedState";

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
        component.Dirty();

        if (TryComp(component.Owner, out AppearanceComponent? appearance))
            appearance.SetData(FoldKey, folded);
    }

    private void OnInsertEvent(EntityUid uid, FoldableComponent component, ContainerGettingInsertedAttemptEvent args)
    {
        if (!component.IsFolded)
            args.Cancel();
    }
}
