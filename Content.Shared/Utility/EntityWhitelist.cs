using System.Collections.Generic;
using Content.Shared.GameObjects.Components.Tag;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Utility
{
    /// <summary>
    ///     Used to determine whether an entity fits a certain whitelist
    /// </summary>
    /// <code>
    /// - whitelist:
    ///   prototypes:
    ///     - FireExtinguisher
    ///     - DisgustingSweptSoup
    ///   tags:
    ///     - Cigarette
    ///     - FirelockElectronics
    ///   components:
    ///     - Buckle
    ///     - AsteroidRock
    /// </code>
    [DataDefinition]
    public class EntityWhitelist : ISerializationHooks
    {
        /// <summary>
        ///     Prototype IDs that are allowed in the whitelist.
        /// </summary>
        [DataField("prototypes")] private readonly string[]? _prototypes = null;

        /// <summary>
        ///     Component names that are allowed in the whitelist.
        /// </summary>
        [DataField("components")] private readonly string[]? _components = null;

        private List<IComponentRegistration>? _registrations = null;

        /// <summary>
        ///     Tags that are allowed in the whitelist.
        /// </summary>
        [DataField("tags")] private readonly string[]? _tags = null;

        void ISerializationHooks.AfterDeserialization()
        {
            if (_components == null) return;

            var compfact = IoCManager.Resolve<IComponentFactory>();
            _registrations = new List<IComponentRegistration>();
            foreach (var name in _components)
            {
                compfact.TryGetRegistration(name, out var registration);
                if (registration == null)
                {
                    Logger.Warning($"Invalid component name {name} passed to EntityWhitelist!");
                    continue;
                }

                _registrations.Add(registration);
            }
        }

        /// <summary>
        ///     Returns whether a given entity fits the whitelist.
        /// </summary>
        public bool IsValid(IEntity entity)
        {
            if (_tags != null)
            {
                if (entity.HasAnyTag(_tags))
                        return true;
            }

            if (_prototypes != null && entity.Prototype != null)
            {
                foreach (var id in _prototypes)
                {
                    if (entity.Prototype.ID == id)
                        return true;
                }
            }
            if (_registrations != null)
            {
                var compfact = IoCManager.Resolve<IComponentFactory>();
                foreach (var reg in _registrations)
                {
                    if (entity.TryGetComponent(reg.Type, out _))
                        return true;
                }
            }
            return false;
        }
    }
}
