using Content.Shared.Popups;
using Content.Shared.Xenoarchaeology.Artifact;
using Content.Shared.Xenoarchaeology.Artifact.Components;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// This is used for...
/// </summary>
public sealed partial class ArtifactDurabilityRestoreEntityEffectsSystem : EntityEffectSystem<XenoArtifactComponent, ArtifactDurabilityRestore>
{
    [Dependency] private readonly SharedXenoArtifactSystem _xenoArtifact = default!;

    protected override void Effect(Entity<XenoArtifactComponent> entity, ref EntityEffectEvent<ArtifactDurabilityRestore> args)
    {
        var durability = args.Effect.RestoredDurability;

        foreach (var node in _xenoArtifact.GetActiveNodes(entity))
        {
            _xenoArtifact.AdjustNodeDurability(node.Owner, durability);
        }
    }
}

public sealed partial class ArtifactUnlockEntityEffectSystem : EntityEffectSystem<XenoArtifactComponent, ArtifactUnlock>
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedXenoArtifactSystem _xenoArtifact = default!;

    protected override void Effect(Entity<XenoArtifactComponent> entity, ref EntityEffectEvent<ArtifactUnlock> args)
    {
        if (EnsureComp<XenoArtifactUnlockingComponent>(entity, out var unlocking))
        {
            if (unlocking.ArtifexiumApplied)
                return;

            _popup.PopupEntity(Loc.GetString("artifact-activation-artifexium"), entity, PopupType.Medium);
        }
        else
        {
            _xenoArtifact.TriggerXenoArtifact(entity, null, force: true);
        }

        _xenoArtifact.SetArtifexiumApplied((entity, unlocking), true);
    }
}

public sealed partial class ArtifactDurabilityRestore : EntityEffectBase<ArtifactDurabilityRestore>
{
    /// <summary>
    ///     Amount of durability that will be restored per effect interaction.
    /// </summary>
    [DataField]
    public int RestoredDurability = 1;
}

public sealed partial class ArtifactUnlock : EntityEffectBase<ArtifactUnlock>;
