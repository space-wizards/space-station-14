using Content.Shared.Examine;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.XAT.Components;

namespace Content.Shared.Xenoarchaeology.Artifact.XAT;

/// <remarks>
/// This isn't an actual trigger but this framework is so fucking nice
/// </remarks>
public sealed class XATExaminableTextSystem : BaseXATSystem<XATExaminableTextComponent>
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        XATSubscribeDirectEvent<ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<XenoArtifactComponent> artifact, Entity<XATExaminableTextComponent, XenoArtifactNodeComponent> node, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString(node.Comp1.ExamineText));
    }
}
