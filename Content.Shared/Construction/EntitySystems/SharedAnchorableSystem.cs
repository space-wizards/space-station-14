using System.Threading.Tasks;
using Content.Shared.Construction.Components;
using Content.Shared.Interaction;
using Content.Shared.Pulling.Components;
using Content.Shared.Tools.Components;

namespace Content.Shared.Construction.EntitySystems;

public abstract class SharedAnchorableSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnchorableComponent, InteractUsingEvent>(OnInteractUsing, after: new[] { typeof(SharedConstructionSystem) });
    }

    private async void OnInteractUsing(EntityUid uid, AnchorableComponent anchorable, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        // If the used entity doesn't have a tool, return early.
        if (!TryComp(args.Used, out ToolComponent? usedTool) || !usedTool.Qualities.Contains(anchorable.Tool))
            return;

        args.Handled = true;
        await TryToggleAnchor(uid, args.User, args.Used, anchorable, usingTool: usedTool);
    }

    public virtual async Task<bool> TryToggleAnchor(EntityUid uid, EntityUid userUid, EntityUid usingUid,
        AnchorableComponent? anchorable = null,
        TransformComponent? transform = null,
        SharedPullableComponent? pullable = null,
        ToolComponent? usingTool = null)
    {
        // Thanks tool system.
        return false;
    }
}
