using Content.Shared.Access.Systems;
using Content.Shared.CharacterAppearance.Components;
using Content.Shared.Ghost;
using Content.Shared.Identity.Components;
using Content.Shared.Inventory;
using Content.Shared.Preferences;
using Robust.Shared.Containers;
using Robust.Shared.Enums;
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

    /// <summary>
    ///     Updates the metadata name for the id(entity) from the current state of the character.
    /// </summary>
    private void UpdateIdentityName(EntityUid uid, IdentityComponent identity)
    {
        if (identity.IdentityEntitySlot.ContainedEntity is not { } ident)
            return;

        var name = GetIdentityName(uid);
        MetaData(ident).EntityName = name;
    }

    private string GetIdentityName(EntityUid target,
        InventoryComponent? inventory=null,
        HumanoidAppearanceComponent? appearance=null)
    {
        var representation = GetIdentityRepresentation(target, inventory, appearance);
        var ev = new SeeIdentityAttemptEvent();

        RaiseLocalEvent(target, ev);
        return representation.ToStringKnown(!ev.Cancelled);
    }

    /// <summary>
    ///     Gets an 'identity representation' of an entity, with their true name being the entity name
    ///     and their 'presumed name' and 'presumed job' being the name/job on their ID card, if they have one.
    /// </summary>
    private IdentityRepresentation GetIdentityRepresentation(EntityUid target,
        InventoryComponent? inventory=null,
        HumanoidAppearanceComponent? appearance=null)
    {
        int age = HumanoidCharacterProfile.MinimumAge;
        Gender gender = Gender.Neuter;

        // Always use their actual age and gender, since that can't really be changed by an ID.
        if (Resolve(target, ref appearance, false))
        {
            gender = appearance.Gender;
            age = appearance.Age;
        }

        var trueName = Name(target);
        if (!Resolve(target, ref inventory, false))
            return new(trueName, age, gender, string.Empty);

        string? presumedJob = null;
        string? presumedName = null;

        // Get their name and job from their ID for their presumed name.
        if (_idCard.TryFindIdCard(target, out var id))
        {
            presumedName = id.FullName;
            presumedJob = id.JobTitle?.ToLowerInvariant();
        }

        // If it didn't find a job, that's fine.
        return new(trueName, age, gender, presumedName, presumedJob);
    }

    #endregion
}

public static class IdentitySystemExtensions
{
    /// <summary>
    ///     Returns the entity that should be used for identity purposes, for example to pass into localization.
    ///     This is an extension method because of its simplicity, and if it was any harder to call it might not
    ///     be used enough.
    /// </summary>
    public static EntityUid IdentityEntity(this EntitySystem sys, EntityUid uid, EntityManager ent)
    {
        if (!ent.TryGetComponent<IdentityComponent>(uid, out var identity))
            return uid;

        return identity.IdentityEntitySlot.ContainedEntity ?? uid;
    }
}
