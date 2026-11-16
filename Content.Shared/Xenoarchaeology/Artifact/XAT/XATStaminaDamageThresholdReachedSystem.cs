using Content.Shared.Damage.Events;
using Content.Shared.Popups;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.XAT.Components;

namespace Content.Shared.Xenoarchaeology.Artifact.XAT;

/// <summary>
/// System for xeno artifact trigger that requires stamina damage to be applied to artifact within a timeframe.
/// </summary>
public sealed partial class XATStaminaDamageThresholdReachedSystem : BaseXATSystem<XATStaminaDamageThresholdReachedComponent>
{
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        XATSubscribeDirectEvent<BeforeStaminaDamageEvent>(OnStaminaDamage);
    }

    /// <summary>
    /// Respond to stamina damage, store amount received over multiple interactions.
    /// If accumulated amount exceeds threshold, trigger artifact and reset.
    /// </summary>
    private void OnStaminaDamage(Entity<XenoArtifactComponent> artifact, Entity<XATStaminaDamageThresholdReachedComponent, XenoArtifactNodeComponent> node, ref BeforeStaminaDamageEvent args)
    {
        if (args.Value <= 0) //prevent stamina regeneration being considered
            return;

        node.Comp1.AccumulatedDamage += args.Value;
        Dirty(node);

        if (node.Comp1.AccumulatedDamage >= node.Comp1.DamageNeeded)
        {
            node.Comp1.AccumulatedDamage = 0;
            Dirty(node);
            Trigger(artifact, node);
        }
        else
        {
            if (node.Comp1.InsufficientDamagePopup != null)
                _popup.PopupEntity(Loc.GetString(node.Comp1.InsufficientDamagePopup), artifact); //tell user they need to interact in the same way more times
        }


    }
}
