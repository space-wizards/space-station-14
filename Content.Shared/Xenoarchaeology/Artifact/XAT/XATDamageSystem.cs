using Content.Shared.Damage;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.XAT.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Xenoarchaeology.Artifact.XAT;

public sealed class XATDamageSystem : BaseXATSystem<XATDamageComponent>
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        XATSubscribeLocalEvent<DamageChangedEvent>(OnDamageChanged);
    }

    private void OnDamageChanged(Entity<XenoArtifactComponent> artifact, Entity<XATDamageComponent, XenoArtifactNodeComponent> node, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased || args.DamageDelta == null)
            return;

        node.Comp1.AccumulatedDamage += args.DamageDelta;

        foreach (var (type, needed) in node.Comp1.TypesNeeded)
        {
            if (node.Comp1.AccumulatedDamage.DamageDict.GetValueOrDefault(type) >= needed)
            {
                node.Comp1.AccumulatedDamage.DamageDict.Clear();
                Dirty(node, node.Comp1);
                Trigger(artifact, node);
                return; // intentional. Do not continue checks
            }
        }

        foreach (var (group, needed) in node.Comp1.GroupsNeeded)
        {
            if (!node.Comp1.AccumulatedDamage.TryGetDamageInGroup(_prototype.Index(group), out var damage))
                continue;

            if (damage >= needed)
            {
                node.Comp1.AccumulatedDamage.DamageDict.Clear();
                Dirty(node, node.Comp1);
                Trigger(artifact, node);
                return; // intentional. Do not continue checks
            }
        }
    }
}
