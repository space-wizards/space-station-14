using System.Numerics;
using Content.Shared._Starlight.Combat.Effects.Components;
using Content.Shared.Armor;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Inventory;
using Content.Shared.Materials;
using Content.Shared.Silicons.Borgs;
using Content.Shared.Weapons.Ranged;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._Starlight.Combat.Effects.EntitySystems;

/// <summary>
/// Handles spawning spark visual effects when armor with high pierce resistance
/// or Rock material is hit by SP or HP hitscan bullets.
/// </summary>
public abstract class SharedArmorSparkEffectSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ArmorSparkEffectComponent, InventoryRelayedEvent<DamageModifyEvent>>(OnArmorDamageModify);
        SubscribeLocalEvent<CyborgSparkEffectComponent, DamageModifyEvent>(OnCyborgDamageModify);
    }

    private void OnArmorDamageModify(EntityUid uid, ArmorSparkEffectComponent component, InventoryRelayedEvent<DamageModifyEvent> args)
    {
        // Only process on server
        if (!_net.IsServer)
            return;

        // Check if this is a hitscan damage event
        if (!IsHitscanDamage(args.Args))
            return;

        // Check if the bullet type is SP or HP
        if (!IsSPOrHPBullet(args.Args))
            return;

        // Check if armor meets the criteria (80%+ pierce resist OR Rock material)
        if (!ArmorMeetsCriteria(uid, args.Args))
            return;

        // Spawn the spark effect
        SpawnSparkEffect(uid, component, args.Args.TargetSlots);
    }

    private bool IsHitscanDamage(DamageModifyEvent args)
    {
        // Check if the damage contains piercing damage (typical for bullets)
        return args.Damage.DamageDict.ContainsKey("Piercing") && args.Damage.DamageDict["Piercing"] > 0;
    }

    private bool IsSPOrHPBullet(DamageModifyEvent args)
    {
        // SP bullets have negative armor penetration (-0.25 to -1)
        // HP bullets have very negative armor penetration (-1)
        // This is a heuristic based on the hitscan prototypes we examined
        return args.ArmorPenetration < 0;
    }

    private bool ArmorMeetsCriteria(EntityUid armorUid, DamageModifyEvent args)
    {
        // Check for Rock material
        if (TryComp<PhysicalCompositionComponent>(armorUid, out var composition))
        {
            if (composition.MaterialComposition.ContainsKey("Rock"))
                return true;
        }

        // Check for 80%+ pierce resistance using CoefficientQueryEvent
        if (HasComp<ArmorComponent>(armorUid))
        {
            var coeffQuery = new CoefficientQueryEvent(SlotFlags.OUTERCLOTHING);
            var relayedEvent = new InventoryRelayedEvent<CoefficientQueryEvent>(coeffQuery);
            RaiseLocalEvent(armorUid, relayedEvent);
            
            if (coeffQuery.DamageModifiers.Coefficients.TryGetValue("Piercing", out var pierceCoeff))
            {
                // Coefficient of 0.2 or less means 80%+ damage reduction
                return pierceCoeff <= 0.2f;
            }
        }

        return false;
    }

    private void SpawnSparkEffect(EntityUid armorUid, ArmorSparkEffectComponent component, SlotFlags targetSlots)
    {
        // Find the entity wearing the armor (the target of the damage)
        var armorTransform = Transform(armorUid);
        var wearer = armorTransform.ParentUid;
        
        if (!Exists(wearer))
            return;

        var wearerTransform = Transform(wearer);
        
        // Calculate random offset within the tile
        var offsetX = _random.NextFloat(-component.MaxOffset, component.MaxOffset);
        var offsetY = _random.NextFloat(-component.MaxOffset, component.MaxOffset);
        var offset = new Vector2(offsetX, offsetY);
        
        // Spawn the effect at the wearer's position with offset
        var effectCoords = wearerTransform.Coordinates.Offset(offset);
        
        SparkEffectAt(effectCoords, component.SparkEffectPrototype, component.RicochetSoundCollection);
    }

    private void OnCyborgDamageModify(EntityUid uid, CyborgSparkEffectComponent component, DamageModifyEvent args)
    {
        // Only process on server
        if (!_net.IsServer)
            return;

        // Check if this is a hitscan damage event
        if (!IsHitscanDamage(args))
            return;

        // For cyborgs, trigger on ANY hitscan bullet (no armor penetration check)
        SpawnCyborgSparkEffect(uid, component);
    }

    private void SpawnCyborgSparkEffect(EntityUid cyborgUid, CyborgSparkEffectComponent component)
    {
        var cyborgTransform = Transform(cyborgUid);
        
        // Calculate random offset within the tile
        var offsetX = _random.NextFloat(-component.MaxOffset, component.MaxOffset);
        var offsetY = _random.NextFloat(-component.MaxOffset, component.MaxOffset);
        var offset = new Vector2(offsetX, offsetY);
        
        // Spawn the effect at the cyborg's position with offset
        var effectCoords = cyborgTransform.Coordinates.Offset(offset);
        
        SparkEffectAt(effectCoords, component.SparkEffectPrototype, component.RicochetSoundCollection);
    }

    private void SparkEffectAt(EntityCoordinates coordinates, string effectPrototype, string soundCollection)
    {
        SpawnSparkEffectAt(coordinates, effectPrototype);
        PlayRicochetSound(coordinates, soundCollection);
    }

    protected abstract void SpawnSparkEffectAt(EntityCoordinates coordinates, string effectPrototype);
    protected abstract void PlayRicochetSound(EntityCoordinates coordinates, string soundCollection);
}
