#nullable enable
using System;
using System.Collections.Generic;
using Content.Shared.Damage;
using Content.Shared.Physics;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.GameObjects.Components.Weapons.Ranged
{
    [Prototype("hitscan")]
    public sealed class HitscanPrototype : IPrototype, IIndexedPrototype
    {
        public string ID { get; private set; } = default!;
        
        // Muzzle -> Travel -> Impact for hitscans forms the full laser.
        // Muzzle is declared elsewhere

        /// <summary>
        ///     Overrides the weapon's muzzle-flash if it uses hitscan.
        /// </summary>
        public string? MuzzleEffect { get; private set; } = "";
        public string? TravelEffect { get; private set; } = "Objects/Weapons/Guns/Projectiles/laser.png";
        public string? ImpactEffect { get; private set; }

        public CollisionGroup CollisionMask { get; private set; } = CollisionGroup.None;

        public float Damage { get; private set; } = 10.0f;

        public DamageType DamageType { get; private set; } = DamageType.Heat;

        public float MaxLength { get; private set; } = 20.0f;
        
        // Sounds
        public string? SoundHitWall { get; private set; } = "/Audio/Weapons/Guns/Hits/laser_sear_wall.ogg";

        public void LoadFrom(YamlMappingNode mapping)
        {
            var serializer = YamlObjectSerializer.NewReader(mapping);

            serializer.DataReadWriteFunction("id", ID, value => ID = value, () => ID);
            serializer.DataReadWriteFunction("muzzleEffect", MuzzleEffect, value => MuzzleEffect = value, () => MuzzleEffect);
            serializer.DataReadWriteFunction("travelEffect", TravelEffect, value => TravelEffect = value, () => TravelEffect);
            serializer.DataReadWriteFunction("impactEffect", ImpactEffect, value => ImpactEffect = value, () => ImpactEffect);
            serializer.DataReadWriteFunction(
                "collisionMask", 
                new List<CollisionGroup> {CollisionGroup.Opaque}, 
                value => value.ForEach(mask => CollisionMask |= mask),
                () =>
                {
                    var result = new List<CollisionGroup>();
                    foreach (var value in (CollisionGroup[]) Enum.GetValues(typeof(CollisionGroup)))
                    {
                        if ((value & CollisionMask) == 0)
                            continue;
                        
                        result.Add(value);
                    }

                    return result;
                });
            serializer.DataReadWriteFunction("damage", Damage, value => Damage = value, () => Damage);
            serializer.DataReadWriteFunction("damageType", DamageType, value => DamageType = value, () => DamageType);
            serializer.DataReadWriteFunction("maxLength", MaxLength, value => MaxLength = value, () => MaxLength);
            
            serializer.DataReadWriteFunction("soundHitWall", SoundHitWall, value => SoundHitWall = value, () => SoundHitWall);
        }
    }
}