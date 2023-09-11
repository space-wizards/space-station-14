using Content.Server.Access.Systems;
using Content.Server.Administration.Logs;
using Content.Server.Humanoid;
using Content.Shared.Database;
using Content.Shared.Hands;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.IdentityManagement;
using Content.Shared.IdentityManagement.Components;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects.Components.Localization;
using Robust.Shared.Prototypes;

namespace Content.Server.IdentityManagement;

/// <summary>
///     Responsible for updating the identity of an entity on init or clothing equip/unequip.
/// </summary>
public class IdentitySystem : SharedIdentitySystem
{
    [Dependency] private readonly IdCardSystem _idCard = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;

    private HashSet<EntityUid> _queuedIdentityUpdates = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IdentityComponent, DidEquipEvent>((uid, _, _) => QueueIdentityUpdate(uid));
        SubscribeLocalEvent<IdentityComponent, DidEquipHandEvent>((uid, _, _) => QueueIdentityUpdate(uid));
        SubscribeLocalEvent<IdentityComponent, DidUnequipEvent>((uid, _, _) => QueueIdentityUpdate(uid));
        SubscribeLocalEvent<IdentityComponent, DidUnequipHandEvent>((uid, _, _) => QueueIdentityUpdate(uid));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var ent in _queuedIdentityUpdates)
        {
            if (!TryComp<IdentityComponent>(ent, out var identity))
                continue;

            UpdateIdentityInfo(ent, identity);
        }

        _queuedIdentityUpdates.Clear();
    }

    // This is where the magic happens
    protected override void OnComponentInit(EntityUid uid, IdentityComponent component, ComponentInit args)
    {
        base.OnComponentInit(uid, component, args);

        var ident = Spawn(null, Transform(uid).Coordinates);

        QueueIdentityUpdate(uid);
        component.IdentityEntitySlot.Insert(ident);
    }

    /// <summary>
    ///     Queues an identity update to the start of the next tick.
    /// </summary>
    public void QueueIdentityUpdate(EntityUid uid)
    {
        _queuedIdentityUpdates.Add(uid);
    }

    #region Private API

    /// <summary>
    ///     Updates the metadata name for the id(entity) from the current state of the character.
    /// </summary>
    private void UpdateIdentityInfo(EntityUid uid, IdentityComponent identity)
    {
        if (identity.IdentityEntitySlot.ContainedEntity is not { } ident)
            return;

        var representation = GetIdentityRepresentation(uid);
        var name = GetIdentityName(uid, representation);

        // Clone the old entity's grammar to the identity entity, for loc purposes.
        if (TryComp<GrammarComponent>(uid, out var grammar))
        {
            var identityGrammar = EnsureComp<GrammarComponent>(ident);
            identityGrammar.Attributes.Clear();

            foreach (var (k, v) in grammar.Attributes)
            {
                identityGrammar.Attributes.Add(k, v);
            }

            // If presumed name is null and we're using that, we set proper noun to be false ("the old woman")
            if (name != representation.TrueName && representation.PresumedName == null)
                identityGrammar.ProperNoun = false;
        }

        if (name == Name(ident))
            return;

        _metaData.SetEntityName(ident, name);

        _adminLog.Add(LogType.Identity, LogImpact.Medium, $"{ToPrettyString(uid)} changed identity to {name}");
        RaiseLocalEvent(new IdentityChangedEvent(uid, ident));
    }

    private string GetIdentityName(EntityUid target, IdentityRepresentation representation)
    {
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
        int age = 18;
        Gender gender = Gender.Epicene;
        string species = SharedHumanoidAppearanceSystem.DefaultSpecies;

        // Always use their actual age and gender, since that can't really be changed by an ID.
        if (Resolve(target, ref appearance, false))
        {
            gender = appearance.Gender;
            age = appearance.Age;
            species = appearance.Species;
        }

        var ageString = _humanoid.GetAgeRepresentation(species, age);
        var trueName = Name(target);
        if (!Resolve(target, ref inventory, false))
            return new(trueName, gender, ageString, string.Empty);

        string? presumedJob = null;
        string? presumedName = null;

        // Get their name and job from their ID for their presumed name.
        if (_idCard.TryFindIdCard(target, out var id))
        {
            presumedName = string.IsNullOrWhiteSpace(id.FullName) ? null : id.FullName;
            presumedJob = id.JobTitle?.ToLowerInvariant();
        }

        // If it didn't find a job, that's fine.
        return new(trueName, gender, ageString, presumedName, presumedJob);
    }

    #endregion
}

public sealed class IdentityChangedEvent : EntityEventArgs
{
    public EntityUid CharacterEntity;
    public EntityUid IdentityEntity;

    public IdentityChangedEvent(EntityUid characterEntity, EntityUid identityEntity)
    {
        CharacterEntity = characterEntity;
        IdentityEntity = identityEntity;
    }
}
