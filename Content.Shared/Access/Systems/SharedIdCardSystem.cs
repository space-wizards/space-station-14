using Content.Shared.Access.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Hands.Components;
using Content.Shared.Inventory;
using Content.Shared.PDA;
using Content.Shared.Roles;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;

namespace Content.Shared.Access.Systems;

public abstract class SharedIdCardSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly MetaDataSystem _metaSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<IdCardComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, IdCardComponent id, MapInitEvent args)
    {
        UpdateEntityName(uid, id);
    }

    /// <summary>
    ///     Attempt to find an ID card on an entity. This will look in the entity itself, in the entity's hands, and
    ///     in the entity's inventory.
    /// </summary>
    public bool TryFindIdCard(EntityUid uid, out Entity<IdCardComponent> idCard)
    {
        // check held item?
        if (TryComp(uid, out HandsComponent? hands) &&
            hands.ActiveHandEntity is EntityUid heldItem &&
            TryGetIdCard(heldItem, out idCard))
        {
            return true;
        }

        // check entity itself
        if (TryGetIdCard(uid, out idCard))
            return true;

        // check inventory slot?
        if (_inventorySystem.TryGetSlotEntity(uid, "id", out var idUid) && TryGetIdCard(idUid.Value, out idCard))
            return true;

        return false;
    }

    /// <summary>
    ///     Attempt to get an id card component from an entity, either by getting it directly from the entity, or by
    ///     getting the contained id from a <see cref="PdaComponent"/>.
    /// </summary>
    public bool TryGetIdCard(EntityUid uid, out Entity<IdCardComponent> idCard)
    {
        if (TryComp(uid, out IdCardComponent? idCardComp))
        {
            idCard = (uid, idCardComp);
            return true;
        }

        if (TryComp(uid, out PdaComponent? pda)
        && TryComp(pda.ContainedId, out idCardComp))
        {
            idCard = (pda.ContainedId.Value, idCardComp);
            return true;
        }

        idCard = default;
        return false;
    }

    /// <summary>
    /// Attempts to change the job title of a card.
    /// Returns true/false.
    /// </summary>
    /// <remarks>
    /// If provided with a player's EntityUid to the player parameter, adds the change to the admin logs.
    /// </remarks>
    public bool TryChangeJobTitle(EntityUid uid, string? jobTitle, IdCardComponent? id = null, EntityUid? player = null)
    {
        if (!Resolve(uid, ref id))
            return false;

        if (!string.IsNullOrWhiteSpace(jobTitle))
        {
            jobTitle = jobTitle.Trim();

            if (jobTitle.Length > IdCardConsoleComponent.MaxJobTitleLength)
                jobTitle = jobTitle[..IdCardConsoleComponent.MaxJobTitleLength];
        }
        else
        {
            jobTitle = null;
        }

        if (id.JobTitle == jobTitle)
            return true;
        id.JobTitle = jobTitle;
        Dirty(uid, id);
        UpdateEntityName(uid, id);

        if (player != null)
        {
            _adminLogger.Add(LogType.Identity, LogImpact.Low,
                $"{ToPrettyString(player.Value):player} has changed the job title of {ToPrettyString(uid):entity} to {jobTitle} ");
        }
        return true;
    }

    public bool TryChangeJobIcon(EntityUid uid, JobIconPrototype jobIcon, IdCardComponent? id = null, EntityUid? player = null)
    {
        if (!Resolve(uid, ref id))
        {
            return false;
        }

        if (id.JobIcon == jobIcon.ID)
        {
            return true;
        }

        id.JobIcon = jobIcon.ID;
        Dirty(uid, id);

        if (player != null)
        {
            _adminLogger.Add(LogType.Identity, LogImpact.Low,
                $"{ToPrettyString(player.Value):player} has changed the job icon of {ToPrettyString(uid):entity} to {jobIcon} ");
        }

        return true;
    }

    public bool TryChangeJobDepartment(EntityUid uid, JobPrototype job, IdCardComponent? id = null)
    {
        if (!Resolve(uid, ref id))
            return false;

        id.JobDepartments.Clear();
        foreach (var department in _prototypeManager.EnumeratePrototypes<DepartmentPrototype>())
        {
            if (department.Roles.Contains(job.ID))
                id.JobDepartments.Add(department.ID);
        }

        Dirty(uid, id);

        return true;
    }

    /// <summary>
    /// Attempts to change the full name of a card.
    /// Returns true/false.
    /// </summary>
    /// <remarks>
    /// If provided with a player's EntityUid to the player parameter, adds the change to the admin logs.
    /// </remarks>
    public bool TryChangeFullName(EntityUid uid, string? fullName, IdCardComponent? id = null, EntityUid? player = null)
    {
        if (!Resolve(uid, ref id))
            return false;

        if (!string.IsNullOrWhiteSpace(fullName))
        {
            fullName = fullName.Trim();
            if (fullName.Length > IdCardConsoleComponent.MaxFullNameLength)
                fullName = fullName[..IdCardConsoleComponent.MaxFullNameLength];
        }
        else
        {
            fullName = null;
        }

        if (id.FullName == fullName)
            return true;
        id.FullName = fullName;
        Dirty(uid, id);
        UpdateEntityName(uid, id);

        if (player != null)
        {
            _adminLogger.Add(LogType.Identity, LogImpact.Low,
                $"{ToPrettyString(player.Value):player} has changed the name of {ToPrettyString(uid):entity} to {fullName} ");
        }
        return true;
    }

    /// <summary>
    /// Changes the name of the id's owner.
    /// </summary>
    /// <remarks>
    /// If either <see cref="FullName"/> or <see cref="JobTitle"/> is empty, it's replaced by placeholders.
    /// If both are empty, the original entity's name is restored.
    /// </remarks>
    private void UpdateEntityName(EntityUid uid, IdCardComponent? id = null)
    {
        if (!Resolve(uid, ref id))
            return;

        var jobSuffix = string.IsNullOrWhiteSpace(id.JobTitle) ? string.Empty : $" ({id.JobTitle})";

        var val = string.IsNullOrWhiteSpace(id.FullName)
            ? Loc.GetString(id.NameLocId,
                ("jobSuffix", jobSuffix))
            : Loc.GetString(id.FullNameLocId,
                ("fullName", id.FullName),
                ("jobSuffix", jobSuffix));
        _metaSystem.SetEntityName(uid, val);
    }
}
