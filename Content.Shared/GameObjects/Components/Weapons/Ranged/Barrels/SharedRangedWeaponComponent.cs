#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Component = Robust.Shared.GameObjects.Component;

namespace Content.Shared.GameObjects.Components.Weapons.Ranged.Barrels
{
    public enum BallisticCaliber
    {
        Unspecified = 0,
        A357, // Placeholder?
        ClRifle,
        SRifle,
        Pistol,
        A35, // Placeholder?
        LRifle,
        Magnum,
        AntiMaterial,
        Shotgun,
        Cap,
        Rocket,
        Dart, // Placeholder
        Grenade,
        Energy,
    }

    /// <summary>
    ///     After the client is done shooting we'll sync how many shots are left just in case.
    /// </summary>
    [Serializable, NetSerializable]
    public class RangedShotsLeftMessage : ComponentMessage
    {
        public int ShotsLeft { get; }

        public RangedShotsLeftMessage(int shotsLeft)
        {
            ShotsLeft = shotsLeft;
        }
    }
    
    [Serializable, NetSerializable]
    public class StartFiringMessage : EntitySystemMessage
    {
        public EntityUid Uid { get; }
        
        public MapCoordinates FireCoordinates { get; }

        public StartFiringMessage(EntityUid uid, MapCoordinates fireCoordinates)
        {
            Uid = uid;
            FireCoordinates = fireCoordinates;
        }
    }

    [Serializable, NetSerializable]
    public sealed class StopFiringMessage : EntitySystemMessage
    {
        public EntityUid Uid { get; }
        
        public int ExpectedShots { get; }

        public StopFiringMessage(EntityUid uid, int expectedShots)
        {
            Uid = uid;
            ExpectedShots = expectedShots;
        }
    }

    [Serializable, NetSerializable]
    public class RangedFireMessage : EntitySystemMessage
    {
        /// <summary>
        ///     Gun Uid
        /// </summary>
        public EntityUid Uid { get; }

        /// <summary>
        ///     Coordinates to shoot at.
        ///     If list empty then we'll stop shooting.
        /// </summary>
        public MapCoordinates FireCoordinates { get; }

        public RangedFireMessage(EntityUid uid, MapCoordinates fireCoordinates)
        {
            Uid = uid;
            FireCoordinates = fireCoordinates;
        }
    }

    public abstract class SharedRangedWeaponComponent : Component, IHandSelected, IInteractUsing, IUse
    {
        /// <summary>
        ///     Current fire selector.
        /// </summary>
        public FireRateSelector Selector { get; protected set; }
        
        /// <summary>
        ///     All available fire selectors
        /// </summary>
        public FireRateSelector AllSelectors { get; protected set; }
        
        /// <summary>
        ///     The earliest time the gun can fire next.
        /// </summary>
        public TimeSpan NextFire { get; protected set; }
        
        /// <summary>
        ///     Shots fired per second.
        /// </summary>
        public float FireRate { get; protected set; }
        
        /// <summary>
        ///     Keep a running track of how many shots we've fired for single-shot (etc.) weapons.
        /// </summary>
        public int ShotCounter;
        
        // These 2 are mainly for handling desyncs so the server fires the same number of shots as the client.
        // Someone smarter probably has a better way of doing it but these seemed to work okay...
        public int ExpectedShots { get; set; }
        
        public int AccumulatedShots { get; set; }

        /// <summary>
        ///     Filepath to MuzzleFlash texture
        /// </summary>
        public string? MuzzleFlash { get; set; }

        public bool Firing { get; set; }

        /// <summary>
        ///     Multiplies the ammo spread to get the final spread of each pellet
        /// </summary>
        public float AmmoSpreadRatio { get; set; }
        
        // Recoil / spray control
        private Angle _minAngle;
        private Angle _maxAngle;
        private Angle _currentAngle = Angle.Zero;
        
        /// <summary>
        ///     How slowly the angle's theta decays per second in radians
        /// </summary>
        private float _angleDecay;
        
        /// <summary>
        ///     How quickly the angle's theta builds for every shot fired in radians
        /// </summary>
        private float _angleIncrease;

        /// <summary>
        ///     How much camera recoil there is.
        /// </summary>
        protected float RecoilMultiplier { get; set; }
        
        public MapCoordinates? FireCoordinates { get; set; }

        // Sounds
        public string? SoundGunshot { get; private set; }
        public string? SoundEmpty { get; private set; }
        
        // Audio profile
        protected const float GunshotVariation = 0.1f;
        protected const float EmptyVariation = 0.1f;
        protected const float CycleVariation = 0.1f;
        protected const float BoltToggleVariation = 0.1f;
        protected const float InsertVariation = 0.1f;

        protected const float GunshotVolume = 0.0f;
        protected const float EmptyVolume = 0.0f;
        protected const float CycleVolume = 0.0f;
        protected const float BoltToggleVolume = 0.0f;
        protected const float InsertVolume = 0.0f;
        
        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            
            serializer.DataReadWriteFunction(
                "fireRate", 
                0.0f, 
                rate => FireRate = rate,
                () => FireRate);
            
            serializer.DataReadWriteFunction(
                "currentSelector", 
                FireRateSelector.Safety, 
                value => Selector = value, 
                () => Selector);
            
            serializer.DataReadWriteFunction(
                "allSelectors", 
                new List<FireRateSelector>(),
                selectors => selectors.ForEach(value => AllSelectors |= value),
                () =>
                {
                    var result = new List<FireRateSelector>();
                    
                    foreach (var selector in (FireRateSelector[]) Enum.GetValues(typeof(FireRateSelector)))
                    {
                        if ((AllSelectors & selector) != 0)
                            result.Add(selector);
                    }

                    return result;
                });
            
            serializer.DataReadWriteFunction("ammoSpreadRatio",
                1.0f,
                value => AmmoSpreadRatio = value,
                () => AmmoSpreadRatio);
            
            // This hard-to-read area's dealing with recoil
            // Use degrees in yaml as it's easier to read compared to "0.0125f"
            serializer.DataReadWriteFunction(
                "minAngle",
                0,
                angle => _minAngle = Angle.FromDegrees(angle / 2f),
                () => _minAngle.Degrees * 2);

            // Random doubles it as it's +/- so uhh we'll just half it here for readability
            serializer.DataReadWriteFunction(
                "maxAngle",
                45,
                angle => _maxAngle = Angle.FromDegrees(angle / 2f),
                () => _maxAngle.Degrees * 2);

            serializer.DataReadWriteFunction(
                "angleIncrease",
                40 / FireRate,
                angle => _angleIncrease = angle * (float) Math.PI / 180f,
                () => MathF.Round(_angleIncrease / ((float) Math.PI / 180f), 2));

            serializer.DataReadWriteFunction(
                "angleDecay",
                20f,
                angle => _angleDecay = angle * (float) Math.PI / 180f,
                () => MathF.Round(_angleDecay / ((float) Math.PI / 180f), 2));

            serializer.DataReadWriteFunction(
                "muzzleFlash",
                "Objects/Weapons/Guns/Projectiles/bullet_muzzle.png",
                value => MuzzleFlash = value,
                () => MuzzleFlash);
            
            serializer.DataReadWriteFunction(
                "recoilMultiplier",
                1.1f,
                value => RecoilMultiplier = value,
                () => RecoilMultiplier);
            
            // Sounds
            serializer.DataReadWriteFunction(
                "soundGunshot",
                null,
                sound => SoundGunshot = sound,
                () => SoundGunshot
                );
            
            serializer.DataReadWriteFunction(
                "soundEmpty",
                "/Audio/Weapons/Guns/Empty/empty.ogg",
                sound => SoundEmpty = sound,
                () => SoundEmpty
            );
        }
        
        public IEntity? Shooter()
        {
            if (!ContainerHelpers.TryGetContainer(Owner, out var container))
                return null;

            return container.Owner;
        }

        protected virtual bool CanFire()
        {
            if (FireRate <= 0.0f)
                return false;
            
            switch (Selector)
            {
                case FireRateSelector.Safety:
                    return false;
                case FireRateSelector.Single:
                    return ShotCounter < 1;
                case FireRateSelector.Automatic:
                    return true;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        ///     Fire out the specified number of bullets if possible.
        ///     Client-side this will just play the specified number of sounds and a muzzle flash.
        ///     Server-side this will work out each bullet to spawn and fire them.
        /// </summary>
        protected virtual bool TryShoot(Angle angle)
        {
            switch (Selector)
            {
                case FireRateSelector.Safety:
                    return false;
                case FireRateSelector.Single:
                    return ShotCounter < 1;
                case FireRateSelector.Automatic:
                    return true;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        ///     Try to shoot the gun for this tick.
        /// </summary>
        /// <param name="currentTime"></param>
        /// <param name="user"></param>
        /// <param name="coordinates"></param>
        /// <param name="firedShots"></param>
        /// <returns>false if firing is impossible, true if firing is possible but delayed or we did fire</returns>
        public bool TryFire(TimeSpan currentTime, IEntity user, MapCoordinates coordinates, out int firedShots)
        {
            firedShots = 0;
            var lastFire = NextFire;
            
            if (ShotCounter == 0 && NextFire <= currentTime)
                NextFire = currentTime;

            if (!CanFire())
                return false;
            
            if (currentTime < NextFire)
                return true;
            
            var fireAngle = (coordinates.Position - user.Transform.WorldPosition).ToAngle();
            var robustRandom = IoCManager.Resolve<IRobustRandom>();

            // To handle guns with firerates higher than framerate / tickrate
            while (NextFire <= currentTime)
            {
                NextFire += TimeSpan.FromSeconds(1 / FireRate);
                var spread = GetWeaponSpread(NextFire, lastFire, fireAngle, robustRandom);
                lastFire = NextFire;
                
                // Mainly check if we can get more bullets (e.g. if there's only 1 left in the clip).
                if (!TryShoot(spread))
                    break;
                
                firedShots++;
                ShotCounter++;
            }
            
            // Somewhat suss on this, needs more playtesting
            if (firedShots == 0)
                return false;
            
            return true;
        }

        /// <summary>
        ///     Get the adjusted weapon angle with recoil
        /// </summary>
        /// <remarks>
        ///     The only reason this is virtual is because client-side randomness isnt deterministic so we can't show an accurate muzzle flash.
        ///     As such (for now) client-side guns will override.
        /// </remarks>
        /// <param name="currentTime"></param>
        /// <param name="lastFire"></param>
        /// <param name="angle"></param>
        /// <param name="robustRandom"></param>
        /// <returns></returns>
        protected virtual Angle GetWeaponSpread(TimeSpan currentTime, TimeSpan lastFire, Angle angle, IRobustRandom robustRandom)
        {
            // TODO: Could also predict this client-side. Probably need to use System.Random and seeds but out of scope for this big pr.
            // If we're sure no desyncs occur then we could just use the Uid to get the seed probably.
            var newTheta = MathHelper.Clamp(
                _currentAngle.Theta + _angleIncrease - _angleDecay * (currentTime - lastFire).TotalSeconds, 
                _minAngle.Theta, 
                _maxAngle.Theta);
            
            _currentAngle = new Angle(newTheta);

            var random = (robustRandom.NextDouble() - 0.5) * 2;
            return Angle.FromDegrees(angle.Degrees + _currentAngle.Degrees * random);
        }

        void IHandSelected.HandSelected(HandSelectedEventArgs eventArgs)
        {
            ResetFire();
        }

        protected void ResetFire()
        {
            ShotCounter = 0;
            NextFire = IoCManager.Resolve<IGameTiming>().CurTime;
        }

        public abstract Task<bool> InteractUsing(InteractUsingEventArgs eventArgs);

        public abstract bool UseEntity(UseEntityEventArgs eventArgs);
    }

    [Flags]
    public enum FireRateSelector
    {
        Safety = 0,
        Single = 1 << 0,
        Automatic = 1 << 1,
    }
}