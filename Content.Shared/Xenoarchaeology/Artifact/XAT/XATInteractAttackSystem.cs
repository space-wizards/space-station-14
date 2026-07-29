using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Whitelist;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.XAT.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Random;

namespace Content.Shared.Xenoarchaeology.Artifact.XAT;

/// <summary>
/// System for xeno artifact trigger for when a projectile collides with this object, or a person attacks it.
/// Damage doesn't matter here, only the act of colliding/attacking.
/// </summary>
public sealed partial class XATInteractAttackSystem : BaseXATSystem<XATInteractAttackComponent>
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private EntityWhitelistSystem _whitelistSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XATInteractAttackComponent, MapInitEvent>(OnMapInit);
        XATSubscribeDirectEvent<StartCollideEvent>(OnStartCollide);
        XATSubscribeDirectEvent<AttackedEvent>(OnAttacked);
        XATSubscribeDirectEvent<HitScanReflectAttemptEvent>(OnHitscan);
    }

    /// <summary>
    /// Randomly decide initial count of interactions for node.
    /// </summary>
    private void OnMapInit(Entity<XATInteractAttackComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.MaxCount = ent.Comp.InteractionCount.Next(_random); //randomly decide count to decrement.
        ent.Comp.Count = ent.Comp.MaxCount; //define count amount.
        Dirty(ent);
    }

    /// <summary>
    /// Trigger the node if the entity used to attack matches the whitelist.
    /// </summary>
    private void OnAttacked(Entity<XenoArtifactComponent> artifact, Entity<XATInteractAttackComponent, XenoArtifactNodeComponent> node, ref AttackedEvent args)
    {
        if (_whitelistSystem.IsWhitelistPassOrNull(node.Comp1.Whitelist, args.Used) && TriggerCountdown(node) == true)
            Trigger(artifact, node);
    }

    /// <summary>
    /// Trigger the node if the colliding entity matches the whitelist.
    /// </summary>
    private void OnStartCollide(Entity<XenoArtifactComponent> artifact, Entity<XATInteractAttackComponent, XenoArtifactNodeComponent> node, ref StartCollideEvent args)
    {
        if (_whitelistSystem.IsWhitelistPassOrNull(node.Comp1.Whitelist, args.OtherEntity) && TriggerCountdown(node) == true)
            Trigger(artifact, node);
    }

    /// <summary>
    /// Trigger the node if the colliding entity matches the whitelist.
    /// </summary>
    private void OnHitscan(Entity<XenoArtifactComponent> artifact, Entity<XATInteractAttackComponent, XenoArtifactNodeComponent> node, ref HitScanReflectAttemptEvent args)
    {
        if (!TryComp<BatteryAmmoProviderComponent>(args.SourceItem, out var batteryComp))
            return;

        if (_whitelistSystem.IsWhitelistPassOrNull(node.Comp1.Whitelist, batteryComp.Prototype) && TriggerCountdown(node))
            Trigger(artifact, node);
    }

    /// <summary>
    /// Count down the number of interactions needed to trigger
    /// <returns>true if enough interactions have been made, false if not</returns>
    private bool TriggerCountdown(Entity<XATInteractAttackComponent> node)
    {
        if (node.Comp.Count <= 1)
        {
            node.Comp.Count = node.Comp.MaxCount;
            Dirty(node);
            return true;
        }
        else
        {
            node.Comp.Count--;
            Dirty(node);
            return false;
        }
    }
}
