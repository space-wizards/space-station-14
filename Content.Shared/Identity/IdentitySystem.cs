using Content.Shared.Access.Systems;
using Content.Shared.Ghost;
using Content.Shared.Identity.Components;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects.Components.Localization;

namespace Content.Shared.Identity;

public sealed partial class IdentitySystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedIdCardSystem _idCard = default!;

    private static string SlotName = "identity";

    public override void Initialize()
    {
        base.Initialize();

        InitializeEvents();
    }

    /// <summary>
    ///     Returns the entity that should be used for identity purposes, for example to pass into localization.
    /// </summary>
    public EntityUid IdentityEntity(EntityUid uid)
    {
        if (!TryComp<IdentityComponent>(uid, out var identity))
            return uid;

        return identity.IdentityEntitySlot.ContainedEntity ?? uid;
    }

    /// <summary>
    ///     Returns the name that should be used for this entity for identity purposes.
    /// </summary>
    public string IdentityName(EntityUid uid, EntityUid? viewer)
    {
        EntityUid entity = uid;
        if (TryComp<IdentityComponent>(uid, out var identity))
        {
            if (viewer == null || !CanSeeThroughIdentity(uid, viewer.Value))
            {
                entity = identity.IdentityEntitySlot.ContainedEntity ?? uid;
            }
        }

        return Name(entity);
    }

    public bool CanSeeThroughIdentity(EntityUid uid, EntityUid viewer)
    {
        if (uid == viewer)
            return true;

        return HasComp<SharedGhostComponent>(viewer);
    }

    #region Private API

    private void UpdateIdentityName(EntityUid uid, IdentityComponent identity)
    {

    }

    #endregion
}
