using Content.Shared.Electrocution;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.XAT.Components;

namespace Content.Shared.Xenoarchaeology.Artifact.XAT;

/// <summary>
/// System for xeno artifact trigger that requires the artifact to be shocked.
/// </summary>
public sealed partial class XATShockSystem : BaseXATSystem<XATShockComponent>
{
    public override void Initialize()
    {
        base.Initialize();

        XATSubscribeDirectEvent<ElectrocutionAttemptEvent>(OnZapped);
    }

    private void OnZapped(Entity<XenoArtifactComponent> artifact, Entity<XATShockComponent, XenoArtifactNodeComponent> node, ref ElectrocutionAttemptEvent args)
    {
        Trigger(artifact, node);
    }
}
