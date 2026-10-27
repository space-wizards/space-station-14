using Content.Shared.Magic;
using Content.Shared.Xenoarchaeology.Artifact.XAE.Components;

namespace Content.Shared.Xenoarchaeology.Artifact.XAE;

/// <summary>
/// System for xeno artifact effect that opens doors in some area around.
/// </summary>
public sealed partial class XAEKnockSystem : BaseXAESystem<XAEKnockComponent>
{
    [Dependency] private SharedMagicSystem _magic = default!;
    /// <inheritdoc />
    protected override void OnActivated(Entity<XAEKnockComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        _magic.Knock(args.Artifact, ent.Comp.KnockRange);
    }
}
