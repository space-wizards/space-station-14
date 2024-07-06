using Content.Shared.Interaction;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.XAT.Components;

namespace Content.Shared.Xenoarchaeology.Artifact.XAT;

/// <summary>
/// This handles <see cref="XATToolUseComponent"/>
/// </summary>
public sealed class XATToolUseSystem : BaseXATSystem<XATToolUseComponent>
{
    [Dependency] private readonly SharedToolSystem _tool = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        XATSubscribeDirectEvent<InteractUsingEvent>(OnInteractUsing);
        XATSubscribeDirectEvent<XATToolUseDoAfterEvent>(OnToolUseComplete);
    }

    private void OnToolUseComplete(Entity<XenoArtifactComponent> artifact, Entity<XATToolUseComponent, XenoArtifactNodeComponent> node, ref XATToolUseDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        if (GetEntity(args.Node) != node.Owner)
            return;

        Trigger(artifact, node);
        args.Handled = true;
    }

    private void OnInteractUsing(Entity<XenoArtifactComponent> artifact, Entity<XATToolUseComponent, XenoArtifactNodeComponent> node, ref InteractUsingEvent args)
    {
        if (!TryComp<ToolComponent>(args.Used, out var tool))
            return;

        args.Handled = _tool.UseTool(args.Used,
            args.User,
            artifact,
            node.Comp1.Delay,
            node.Comp1.NeededTool,
            new XATToolUseDoAfterEvent(GetNetEntity(node)),
            fuel: node.Comp1.Fuel,
            tool);
    }
}
