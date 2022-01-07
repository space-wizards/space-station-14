using Content.Shared.Item;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Shared.Foldable;

[UsedImplicitly]
public abstract class SharedFoldableSystem : EntitySystem
{
    [Dependency] private SharedContainerSystem _container = default!;

    private const string FoldKey = "FoldedState";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FoldableComponent, ComponentInit>(OnFoldableInit);
        SubscribeLocalEvent<FoldableComponent, AttemptItemPickupEvent>(OnPickedUpAttempt);
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
        component.Dirty();
        component.IsFolded = folded;
        component.CanBeFolded = !_container.IsEntityInContainer(component.Owner);

        // Update visuals only if the value has changed
        if (EntityManager.TryGetComponent(component.Owner, out AppearanceComponent? appearance))
            appearance.SetData(FoldKey, folded);
    }

    /// <summary>
    /// Prevents foldable objects to be picked up when unfolded
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    /// <param name="args"></param>
    private void OnPickedUpAttempt(EntityUid uid, FoldableComponent component, AttemptItemPickupEvent args)
    {
        if (!component.IsFolded)
            args.Cancel();
    }
}
