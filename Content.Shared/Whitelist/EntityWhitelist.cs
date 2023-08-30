using Content.Shared.Tag;
using Robust.Shared.Serialization;
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
    [Serializable, NetSerializable]
    public sealed partial class EntityWhitelist
    {
        /// <summary>
        ///     Component names that are allowed in the whitelist.
        /// </summary>
        [DataField("components")] public string[]? Components = null;
        // TODO yaml validation

        [NonSerialized]
        private List<ComponentRegistration>? _registrations = null;

        /// <summary>
        ///     Tags that are allowed in the whitelist.
        /// </summary>
        [DataField("tags", customTypeSerializer:typeof(PrototypeIdListSerializer<TagPrototype>))]
        public List<string>? Tags = null;

        /// <summary>
        ///     If false, an entity only requires one of these components or tags to pass the whitelist. If true, an
        ///     entity requires to have ALL of these components and tags to pass.
        /// </summary>
        [DataField("requireAll")]
        public bool RequireAll = false;

        public void UpdateRegistrations()
        {
            if (Components == null) return;

            var compfact = IoCManager.Resolve<IComponentFactory>();
            _registrations = new List<ComponentRegistration>();
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
            if (Components != null && _registrations == null)
                UpdateRegistrations();

            IoCManager.Resolve(ref entityManager);
            if (_registrations != null)
            {
                foreach (var reg in _registrations)
                {
                    if (entityManager.HasComponent(uid, reg.Type))
                    {
                        if (!RequireAll)
                            return true;
                    }
                    else if (RequireAll)
                        return false;
                }
            }

            if (Tags != null && entityManager.TryGetComponent(uid, out TagComponent? tags))
            {
                var tagSystem = entityManager.System<TagSystem>();
                return RequireAll ? tagSystem.HasAllTags(tags, Tags) : tagSystem.HasAnyTag(tags, Tags);
            }

            return false;
        }
    }
}
