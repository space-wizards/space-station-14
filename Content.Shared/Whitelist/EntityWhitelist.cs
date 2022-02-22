using System.Collections.Generic;
using Content.Shared.Tag;
using Content.Shared.Wires;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Whitelist
{
    /// <summary>
    ///     Used to determine whether an entity fits a certain whitelist.
    ///     Does not whitelist by prototypes, since that is undesirable; you're better off just adding a tag to all
    ///     entity prototypes that need to be whitelisted, and checking for that.
    /// </summary>
    /// <code>
    /// whitelist:
    ///   tags:
    ///     - Cigarette
    ///     - FirelockElectronics
    ///   components:
    ///     - Buckle
    ///     - AsteroidRock
    /// </code>
    [DataDefinition]
    public sealed class EntityWhitelist : ISerializationHooks
    {
        /// <summary>
        ///     Component names that are allowed in the whitelist.
        /// </summary>
        [DataField("components")] public string[]? Components = null;

        private List<IComponentRegistration>? _registrations = null;

        /// <summary>
        ///     Tags that are allowed in the whitelist.
        /// </summary>
        [DataField("tags")]
        public string[]? Tags = null;

        void ISerializationHooks.AfterDeserialization()
        {
            UpdateRegistrations();
        }

        public void UpdateRegistrations()
        {
            if (Components == null) return;

            var compfact = IoCManager.Resolve<IComponentFactory>();
            _registrations = new List<IComponentRegistration>();
            foreach (var name in Components)
            {
                var availability = compfact.GetComponentAvailability(name);
                if (compfact.TryGetRegistration(name, out var registration)
                    && availability == ComponentAvailability.Available)
                {
                    _registrations.Add(registration);
                }
                else if (availability == ComponentAvailability.Unknown)
                {
                    Logger.Warning($"Unknown component name {name} passed to EntityWhitelist!");
                }
            }
        }

        /// <summary>
        ///     Returns whether a given entity fits the whitelist.
        /// </summary>
        public bool IsValid(EntityUid uid, IEntityManager? entityManager = null)
        {
            entityManager ??= IoCManager.Resolve<IEntityManager>();
            var tagSystem = EntitySystem.Get<TagSystem>();

            if (Tags != null && entityManager.TryGetComponent(uid, out TagComponent? tags))
            {
                if (tagSystem.HasAnyTag(tags, Tags))
                        return true;
            }

            if (_registrations != null)
            {
                foreach (var reg in _registrations)
                {
                    if (entityManager.HasComponent(uid, reg.Type))
                        return true;
                }
            }
            return false;
        }
    }
}
