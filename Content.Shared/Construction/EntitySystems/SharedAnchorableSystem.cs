using Content.Shared.Construction.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Pulling.Components;
using Content.Shared.Tools.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.Construction.EntitySystems;

public abstract class SharedAnchorableSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnchorableComponent, InteractUsingEvent>(OnInteractUsing,
            before: new[] { typeof(ItemSlotsSystem) }, after: new[] { typeof(SharedConstructionSystem) });
    }

    private void OnInteractUsing(EntityUid uid, AnchorableComponent anchorable, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        // If the used entity doesn't have a tool, return early.
        if (!TryComp(args.Used, out ToolComponent? usedTool) || !usedTool.Qualities.Contains(anchorable.Tool))
            return;

        args.Handled = true;
        TryToggleAnchor(uid, args.User, args.Used, anchorable, usingTool: usedTool);
    }

    public virtual void TryToggleAnchor(EntityUid uid, EntityUid userUid, EntityUid usingUid,
        AnchorableComponent? anchorable = null,
        TransformComponent? transform = null,
        SharedPullableComponent? pullable = null,
        ToolComponent? usingTool = null)
    {
        // Thanks tool system.

        // TODO tool system is fixed now, make this actually shared.
    }

    [Serializable, NetSerializable]
    protected sealed class TryUnanchorCompletedEvent : SimpleDoAfterEvent
    {
    }

    [Serializable, NetSerializable]
    protected sealed class TryAnchorCompletedEvent : SimpleDoAfterEvent
    {
    }
}
