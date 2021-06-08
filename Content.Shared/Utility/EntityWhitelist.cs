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
    /// whitelist:
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
        [DataField("prototypes")] public string[]? Prototypes = null;

        /// <summary>
        ///     Component names that are allowed in the whitelist.
        /// </summary>
        [DataField("components")] public string[]? Components = null;

        private List<IComponentRegistration>? _registrations = null;

        /// <summary>
        ///     Tags that are allowed in the whitelist.
        /// </summary>
        [DataField("tags")] public string[]? Tags = null;

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
            if (Tags != null)
            {
                if (entity.HasAnyTag(Tags))
                        return true;
            }

            if (Prototypes != null && entity.Prototype != null)
            {
                foreach (var id in Prototypes)
                {
                    if (entity.Prototype.ID == id)
                        return true;
                }
            }
            if (_registrations != null)
            {
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
