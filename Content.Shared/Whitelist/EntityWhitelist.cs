using Content.Shared.Item;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Whitelist;

/// <summary>
///     Used to determine whether an entity fits a certain whitelist.
///     Does not whitelist by prototypes, since that is undesirable; you're better off just adding a tag to all
///     entity prototypes that need to be whitelisted, and checking for that.
/// </summary>
/// <code>
/// whitelist:
///   tags:
///   - Cigarette
///   - FirelockElectronics
///   components:
///   - Buckle
///   - AsteroidRock
///   sizes:
///   - Tiny
///   - Large
/// </code>
[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class EntityWhitelist
{
    /// <summary>
    ///     Component names that are allowed in the whitelist.
    /// </summary>
    [DataField] public string[]? Components;
    // TODO yaml validation

    /// <summary>
    ///     Item sizes that are allowed in the whitelist.
    /// </summary>
    [DataField]
    public List<ProtoId<ItemSizePrototype>>? Sizes;

    [NonSerialized]
    private List<ComponentRegistration>? _registrations;

    /// <summary>
    ///     Tags that are allowed in the whitelist.
    /// </summary>
    [DataField]
    public List<ProtoId<TagPrototype>>? Tags;

    /// <summary>
    ///     If false, an entity only requires one of these components or tags to pass the whitelist. If true, an
    ///     entity requires to have ALL of these components and tags to pass.
    ///     The "Sizes" criteria will ignores this, since an item can only have one size.
    /// </summary>
    [DataField]
    public bool RequireAll;

    public void UpdateRegistrations()
    {

        if (Components == null)
            return;

        var compFact = IoCManager.Resolve<IComponentFactory>();
        _registrations = new List<ComponentRegistration>();
        foreach (var name in Components)
        {
            var availability = compFact.GetComponentAvailability(name);
            if (compFact.TryGetRegistration(name, out var registration)
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

        if (Sizes != null && entityManager.TryGetComponent(uid, out ItemComponent? itemComp))
        {
            if (Sizes.Contains(itemComp.Size))
                return true;
        }

        if (Tags != null && entityManager.TryGetComponent(uid, out TagComponent? tags))
        {
            var tagSystem = entityManager.System<TagSystem>();
            return RequireAll ? tagSystem.HasAllTags(tags, Tags) : tagSystem.HasAnyTag(tags, Tags);
        }

        if (RequireAll)
            return true;

        return false;
    }
}
