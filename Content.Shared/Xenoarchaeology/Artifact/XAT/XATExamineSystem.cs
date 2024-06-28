using Content.Shared.Examine;
using Content.Shared.Ghost;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.XAT.Components;

namespace Content.Shared.Xenoarchaeology.Artifact.XAT;

public sealed class XATExamineSystem : BaseXATSystem<XATExamineComponent>
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        XATSubscribeLocalEvent<ExaminedEvent>(OnExamine);
    }

    private void OnExamine(Entity<XenoArtifactComponent> artifact, Entity<XATExamineComponent, XenoArtifactNodeComponent> node, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (TryComp<GhostComponent>(args.Examiner, out var ghost) && !ghost.CanGhostInteract)
            return;

        Trigger(artifact, node);
        args.AddMarkup(Loc.GetString("artifact-examine-trigger-desc"));
    }
}
