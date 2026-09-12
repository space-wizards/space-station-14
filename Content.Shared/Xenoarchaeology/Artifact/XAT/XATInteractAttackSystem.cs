using Content.Shared.Popups;
using Content.Shared.Weapons.Hitscan.Events;
using Content.Shared.Weapons.Melee.Events;
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
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private EntityWhitelistSystem _whitelistSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        XATSubscribeDirectEvent<AttackedEvent>(OnAttacked);
        XATSubscribeDirectEvent<StartCollideEvent>(OnStartCollide);
        XATSubscribeDirectEvent<HitscanRaycastStrikeEvent>(OnHitscan);
    }

    /// <summary>
    /// Randomly decide initial count of interactions for node.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnMapInit(Entity<XATInteractAttackComponent> ent, ref MapInitEvent args)
    {
        SetMaxCount(ent);
    }

    /// <summary>
    /// Trigger the node if the entity used to attack matches the whitelist.
    /// </summary>
    private void OnAttacked(Entity<XenoArtifactComponent> artifact, Entity<XATInteractAttackComponent, XenoArtifactNodeComponent> node, ref AttackedEvent args)
    {
        if (_whitelistSystem.IsWhitelistPassOrNull(node.Comp1.Whitelist, args.Used) && DoTriggerCountdown(node, artifact.Owner, args.User))
            Trigger(artifact, node);
    }

    /// <summary>
    /// Trigger the node if the colliding entity matches the whitelist.
    /// </summary>
    private void OnStartCollide(Entity<XenoArtifactComponent> artifact, Entity<XATInteractAttackComponent, XenoArtifactNodeComponent> node, ref StartCollideEvent args)
    {
        if (_whitelistSystem.IsWhitelistPassOrNull(node.Comp1.Whitelist, args.OtherEntity) && DoTriggerCountdown(node, artifact.Owner, args.OtherEntity))
            Trigger(artifact, node);
    }

    /// <summary>
    /// Trigger the node if the striking hitscan matches the whitelist.
    /// </summary>
    private void OnHitscan(Entity<XenoArtifactComponent> artifact, Entity<XATInteractAttackComponent, XenoArtifactNodeComponent> node, ref HitscanRaycastStrikeEvent args)
    {
        if ((_whitelistSystem.IsWhitelistPassOrNull(node.Comp1.Whitelist, args.Data.Hitscan)
             || _whitelistSystem.IsWhitelistPassOrNull(node.Comp1.Whitelist, args.Data.Gun))
            && DoTriggerCountdown(node, artifact.Owner, args.Data.Shooter))
        {
            Trigger(artifact, node);
        }
    }

    /// <summary>
    /// Count down the number of interactions needed to trigger.
    /// </summary>
    /// <returns>True if enough interactions have been made, False if not.</returns>
    private bool DoTriggerCountdown(Entity<XATInteractAttackComponent> ent, EntityUid artifact, EntityUid? user)
    {
        if (ent.Comp.MaxCount == null || ent.Comp.Count == null) //ensure countdown isn't null
            SetMaxCount(ent);

        ent.Comp.Count--;

        if (ent.Comp.Count > 0)
        {
            if (ent.Comp.InsufficientInteractionPopup != null)
                _popup.PopupEntity(Loc.GetString(ent.Comp.InsufficientInteractionPopup), artifact, user); //tell user they need to interact in the same way more times

            Dirty(ent);
            return false;
        }

        ent.Comp.Count = ent.Comp.MaxCount;
        Dirty(ent);
        return true;
    }

    /// <summary>
    /// Sets the maximum value to count down to a random number if not otherwise defined.
    /// Sets the count value if not defined.
    /// </summary>
    private void SetMaxCount(Entity<XATInteractAttackComponent> ent)
    {
        if (ent.Comp.MaxCount == null)
            ent.Comp.MaxCount = ent.Comp.InteractionCount.Next(_random); // randomly decide count to decrement.

        if (ent.Comp.Count == null)
            ent.Comp.Count = ent.Comp.MaxCount.Value; // define count amount.

        Dirty(ent);
    }
}
