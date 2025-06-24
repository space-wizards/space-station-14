using System.Globalization;
using Content.Shared.Access.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.Hands.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.PDA;
using Content.Shared.Roles;
using Content.Shared.StatusIcon;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Access.Systems;

public abstract class SharedIdCardSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfgManager = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAccessSystem _access = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly MetaDataSystem _metaSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    // CCVar.
    private int _maxNameLength;
    private int _maxIdJobLength;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IdCardComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<TryGetIdentityShortInfoEvent>(OnTryGetIdentityShortInfo);
        SubscribeLocalEvent<EntityRenamedEvent>(OnRename);

        Subs.CVar(_cfgManager, CCVars.MaxNameLength, value => _maxNameLength = value, true);
        Subs.CVar(_cfgManager, CCVars.MaxIdJobLength, value => _maxIdJobLength = value, true);
    }

    private void OnRename(ref EntityRenamedEvent ev)
    {
        // When a player gets renamed their id card is renamed as well to match.
        // Unfortunately since TryFindIdCard will succeed if the entity is also a card this means that the card will
        // keep renaming itself unless we return early.
        // We also do not include the PDA itself being renamed, as that triggers the same event (e.g. for chameleon PDAs).
        if (HasComp<IdCardComponent>(ev.Uid) || HasComp<PdaComponent>(ev.Uid))
            return;

        if (TryFindIdCard(ev.Uid, out var idCard))
            TryChangeFullName(idCard, ev.NewName, idCard);
    }

    private void OnMapInit(EntityUid uid, IdCardComponent id, MapInitEvent args)
    {
        UpdateEntityName(uid, id);
    }

    private void OnTryGetIdentityShortInfo(TryGetIdentityShortInfoEvent ev)
    {
        if (ev.Handled)
        {
            return;
        }

        string? title = null;
        if (TryFindIdCard(ev.ForActor, out var idCard) && !(ev.RequestForAccessLogging && idCard.Comp.BypassLogging))
        {
            title = ExtractFullTitle(idCard);
        }

        ev.Title = title;
        ev.Handled = true;
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
    /// Actually works with the LocalizedJobTitle DataField and not with JobTitle.
    /// </remarks>
    public bool TryChangeJobTitle(EntityUid uid, string? jobTitle, IdCardComponent? id = null, EntityUid? player = null)
    {
        if (!Resolve(uid, ref id))
            return false;

        if (!string.IsNullOrWhiteSpace(jobTitle))
        {
            jobTitle = jobTitle.Trim();

            if (jobTitle.Length > _maxIdJobLength)
                jobTitle = jobTitle[.._maxIdJobLength];
        }
        else
        {
            jobTitle = null;
        }

        if (id.LocalizedJobTitle == jobTitle)
            return true;
        id.LocalizedJobTitle = jobTitle;
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

    public bool TryChangeJobDepartment(EntityUid uid, List<ProtoId<DepartmentPrototype>> departments, IdCardComponent? id = null)
    {
        if (!Resolve(uid, ref id))
            return false;

        id.JobDepartments.Clear();
        foreach (var department in departments)
        {
            id.JobDepartments.Add(department);
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
            if (fullName.Length > _maxNameLength)
                fullName = fullName[.._maxNameLength];
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

        var jobSuffix = string.IsNullOrWhiteSpace(id.LocalizedJobTitle) ? string.Empty : $" ({id.LocalizedJobTitle})";

        var val = string.IsNullOrWhiteSpace(id.FullName)
            ? Loc.GetString(id.NameLocId,
                ("jobSuffix", jobSuffix))
            : Loc.GetString(id.FullNameLocId,
                ("fullName", id.FullName),
                ("jobSuffix", jobSuffix));
        _metaSystem.SetEntityName(uid, val);
    }

    private static string ExtractFullTitle(IdCardComponent idCardComponent)
    {
        return $"{idCardComponent.FullName} ({CultureInfo.CurrentCulture.TextInfo.ToTitleCase(idCardComponent.LocalizedJobTitle ?? string.Empty)})"
            .Trim();
    }

    public void SetExpireTime(Entity<ExpireIdCardComponent?> ent, TimeSpan time)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;
        ent.Comp.ExpireTime = time;
        Dirty(ent);
    }

    public void SetPermanent(Entity<ExpireIdCardComponent?> ent, bool val)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;
        ent.Comp.Permanent = val;
        Dirty(ent);
    }

    /// <summary>
    /// Marks an <see cref="ExpireIdCardComponent"/> as expired, setting the accesses.
    /// </summary>
    public virtual void ExpireId(Entity<ExpireIdCardComponent> ent)
    {
        if (ent.Comp.Expired)
            return;

        _access.TrySetTags(ent, ent.Comp.ExpiredAccess);
        ent.Comp.Expired = true;
        Dirty(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<ExpireIdCardComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Expired || comp.Permanent)
                continue;

            if (_timing.CurTime < comp.ExpireTime)
                continue;

            ExpireId((uid, comp));
        }
    }
}
