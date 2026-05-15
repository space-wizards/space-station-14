using Content.Shared.Examine;
using Content.Shared.Ghost;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.XAT.Components;

namespace Content.Shared.Xenoarchaeology.Artifact.XAT;

/// <summary>
/// System for xeno artifact trigger that requires player to examine details of artifact.
/// </summary>
public sealed class XATExamineSystem : BaseXATSystem<XATExamineComponent>
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        XATSubscribeDirectEvent<ExaminedEvent>(OnExamine);
    }

    private void OnExamine(Entity<XenoArtifactComponent> artifact, Entity<XATExamineComponent, XenoArtifactNodeComponent> node, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (HasComp<GhostComponent>(args.Examiner))
            return;

        Trigger(artifact, node);
        args.PushMarkup(Loc.GetString("artifact-examine-trigger-desc"));
    }
}
