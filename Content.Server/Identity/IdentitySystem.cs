using Content.Server.Access.Systems;
using Content.Shared.CharacterAppearance.Components;
using Content.Shared.Identity;
using Content.Shared.Identity.Components;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Preferences;
using Robust.Shared.Containers;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects.Components.Localization;

namespace Content.Server.Identity;

/// <summary>
///     Responsible for updating the identity of an entity on init or clothing equip/unequip.
/// </summary>
public class IdentitySystem : SharedIdentitySystem
{
    [Dependency] private readonly IdCardSystem _idCard = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IdentityComponent, DidEquipEvent>(OnEquip);
        SubscribeLocalEvent<IdentityComponent, DidUnequipEvent>(OnUnequip);
    }

    // This is where the magic happens
    protected override void OnComponentInit(EntityUid uid, IdentityComponent component, ComponentInit args)
    {
        var ident = Spawn(null, Transform(uid).Coordinates);

        // Clone the old entity's grammar to the identity entity, for loc purposes.
        if (TryComp<GrammarComponent>(uid, out var grammar))
        {
            var identityGrammar = EnsureComp<GrammarComponent>(ident);

            foreach (var (k, v) in grammar.Attributes)
            {
                identityGrammar.Attributes.Add(k, v);
            }
        }

        MetaData(ident).EntityName = Name(uid);
        component.IdentityEntitySlot.Insert(ident);
    }

    private void OnEquip(EntityUid uid, IdentityComponent component, DidEquipEvent args)
    {
        UpdateIdentityName(uid, component);
    }

    private void OnUnequip(EntityUid uid, IdentityComponent component, DidUnequipEvent args)
    {
        UpdateIdentityName(uid, component);
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
