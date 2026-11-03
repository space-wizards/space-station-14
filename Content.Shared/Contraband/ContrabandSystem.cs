using Content.Shared.Access.Systems;
using Content.Shared.CCVar;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Localizations;
using Content.Shared.Roles;
using Content.Shared.Verbs;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects.Components.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Shared.Contraband;

/// <summary>
/// This handles showing examine messages for contraband-marked items.
/// </summary>
public sealed partial class ContrabandSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _configuration = default!;
    [Dependency] private IEntityManager _entity = default!;
    [Dependency] private SharedIdCardSystem _id = default!;
    [Dependency] private ExamineSystemShared _examine = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    private bool _contrabandExamineEnabled;
    private bool _contrabandExamineOnlyInHudEnabled;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ContrabandComponent, GetVerbsEvent<ExamineVerb>>(OnDetailedExamine);

        Subs.CVar(_configuration, CCVars.ContrabandExamine, SetContrabandExamine, true);
        Subs.CVar(_configuration, CCVars.ContrabandExamineOnlyInHUD, SetContrabandExamineOnlyInHUD, true);
    }

    public void CopyDetails(EntityUid uid, ContrabandComponent other, ContrabandComponent? contraband = null)
    {
        if (!Resolve(uid, ref contraband))
            return;

        contraband.Severity = other.Severity;
        contraband.AllowedDepartments = other.AllowedDepartments;
        contraband.AllowedJobs = other.AllowedJobs;
        Dirty(uid, contraband);
    }

    private void OnDetailedExamine(EntityUid ent, ContrabandComponent component, ref GetVerbsEvent<ExamineVerb> args)
    {

        if (!_contrabandExamineEnabled)
            return;

        // CanAccess is not used here, because we want people to be able to examine legality in strip menu.
        if (!args.CanInteract)
            return;

        // if contraband is only supposed to be shown in the HUD, make sure we can see it.
        if (_contrabandExamineOnlyInHudEnabled)
        {
            var ev = new GetContrabandDetailsEvent();
            RaiseLocalEvent(args.User, ref ev);
            if (!ev.CanShowContraband)
                return;
        }

        // two strings:
        // one, the actual informative 'this is restricted' 
        var severity = ProtoMan.Index(component.Severity);
        string departmentExamineMessage;
        if (severity.ShowDepartmentsAndJobs)
        {
            // department restricted text
            departmentExamineMessage =
                GenerateDepartmentExamineMessage(component.AllowedDepartments, component.AllowedJobs, severity.Color);
        }
        else
        {
            departmentExamineMessage = Loc.GetString(severity.ExamineText, ("type", ContrabandItemType.Item), ("color", severity.Color.ToHex()));
        }

        // if it is fully restricted, you're department-less, or your department isn't in the allowed list, you cannot carry it. Otherwise, you can.
        //var iconTexture = IsContraband((ent, component), args.User, out _) ? "/Textures/Interface/VerbIcons/Contraband/locked.color." + severity.ID + ".svg.192dpi.png" : "/Textures/Interface/VerbIcons/Contraband/unlock.color." + severity.ID + ".svg.192dpi.png";

        // then, the 'you can/shouldn't carry this around' based on the ID the user is wearing
        (string carryingMessage, string iconTexture) = GetLegalityContent((ent, component), args.User, severity.ID);

        var examineMarkup = GetContrabandExamine(departmentExamineMessage, carryingMessage);

        _examine.AddHoverExamineVerb(args,
            component,
            Loc.GetString("contraband-examinable-verb-text"),
            examineMarkup.ToMarkup(),
            iconTexture);
    }

    private (string, string) GetLegalityContent(Entity<ContrabandComponent> item, EntityUid examiner, string severity)
    {

        // Checks if the contraband is contained within a container. if NOT, do you.
        if (!_container.TryGetContainingContainer((item.Owner, null, null), out var ctnr))
        {
            return (ResolveLegalityText(item, examiner, true), ResolveIconPath(item, examiner, severity));
        }

        if (ctnr is { })
        {
            // if you are examining your own item, do you.
            if (examiner == ctnr.Owner)
            {
                return (ResolveLegalityText(item, examiner, true), ResolveIconPath(item, examiner, severity));
            }

            // holder has neither pronouns nor id, do you.
            if (!HasComp<GrammarComponent>(ctnr.Owner) && !_id.TryFindIdCard(ctnr.Owner, out _))
            {
                return (ResolveLegalityText(item, examiner, true), ResolveIconPath(item, examiner, severity));
            }

            return (ResolveLegalityText(item, ctnr.Owner, false), ResolveIconPath(item, ctnr.Owner, severity));
        }
        return (ResolveLegalityText(item, examiner, true), ResolveIconPath(item, examiner, severity)); //if something fucked up, default to you.
    }

    private string ResolveLegalityText(Entity<ContrabandComponent> item, EntityUid idOwner, bool you)
    {
        return Loc.GetString(IsContraband((item.Owner, item.Comp), idOwner, out _) ? "contraband-examine-text-avoid-carrying-around" : "contraband-examine-text-in-the-clear", ("you", you ? "true" : "false"), ("gender", Identity.Entity(idOwner, _entity)));
    }

    private string ResolveIconPath(Entity<ContrabandComponent> item, EntityUid? idOwner, string severity)
    {
        return IsContraband((item.Owner, item.Comp), idOwner, out _) ? "/Textures/Interface/VerbIcons/Contraband/locked.color." + severity + ".svg.192dpi.png" : "/Textures/Interface/VerbIcons/Contraband/unlock.color." + severity + ".svg.192dpi.png";
    }

    /// <summary>
    /// Create an examine message from the given inputs!
    /// </summary>
    /// <param name="allowedDepartments">What departments this contraband is allowed in.</param>
    /// <param name="allowedJobs">What jobs this contraband is allowed in.</param>
    /// <param name="color">The color of the text.</param>
    /// <param name="itemType">The type of entity (item, reagent etc...)</param>
    /// <returns>A localized string with the formatted message</returns>
    public string GenerateDepartmentExamineMessage(HashSet<ProtoId<DepartmentPrototype>> allowedDepartments, HashSet<ProtoId<JobPrototype>> allowedJobs, Color color, ContrabandItemType itemType = ContrabandItemType.Item)
    {
        var localizedDepartments = allowedDepartments.Select(p => Loc.GetString("contraband-department-plural", ("department", Loc.GetString(ProtoMan.Index(p).Name))));
        var jobs = allowedJobs.Select(p => ProtoMan.Index(p).LocalizedName).ToArray();
        var localizedJobs = jobs.Select(p => Loc.GetString("contraband-job-plural", ("job", p)));

        //creating a combined list of jobs and departments for the restricted text
        var list = ContentLocalizationManager.FormatList(localizedDepartments.Concat(localizedJobs).ToList());

        // department restricted text
        return Loc.GetString("contraband-examine-text-Restricted-department", ("departments", list), ("type", itemType), ("color", color.ToHex()));
    }

    private FormattedMessage GetContrabandExamine(string deptMessage, string carryMessage)
    {
        var msg = new FormattedMessage();
        msg.AddMarkupOrThrow(deptMessage);
        msg.PushNewline();
        msg.AddMarkupOrThrow(carryMessage);
        return msg;
    }

    private void SetContrabandExamine(bool val)
    {
        _contrabandExamineEnabled = val;
    }

    private void SetContrabandExamineOnlyInHUD(bool val)
    {
        _contrabandExamineOnlyInHudEnabled = val;
    }

    /// <summary>
    /// Determines if an item is contraband for a given player. If no player is provided, will just return if the item
    /// is contraband in general.
    /// </summary>
    /// <param name="contraband">The entity that we are checking for contraband.</param>
    /// <param name="player">The player that we are checking if they are allowed to have this contraband.</param>
    /// <param name="contraProtoId">The contraband ProtoId if the item is contraband.</param>
    /// <returns></returns>
    public bool IsContraband(Entity<ContrabandComponent?> contraband, EntityUid? player, [NotNullWhen(true)] out ProtoId<ContrabandSeverityPrototype>? contraProtoId)
    {
        contraProtoId = null;

        if (!Resolve(contraband.Owner, ref contraband.Comp, false))
            return false;

        contraProtoId = contraband.Comp.Severity;

        if (player == null)
            return true;

        List<ProtoId<DepartmentPrototype>> departments = new();
        var jobId = "";
        if (_id.TryFindIdCard(player.Value, out var id))
        {
            departments = id.Comp.JobDepartments;
            if (id.Comp.LocalizedJobTitle is not null)
                jobId = id.Comp.LocalizedJobTitle;
        }

        var jobs = contraband.Comp.AllowedJobs.Select(p => ProtoMan.Index(p).LocalizedName).ToArray();
        // if it is fully restricted, you're department-less, or your department isn't in the allowed list, you cannot carry it. Otherwise, you can.
        if (departments.Intersect(contraband.Comp.AllowedDepartments).Any() || jobs.Contains(jobId))
            return false;

        return true;
    }

    /// <summary>
    /// Checks if a storage has contraband.
    /// </summary>
    /// <param name="contraband">The entity that we are checking for contraband.</param>
    /// <param name="player">The player that we are checking if they are allowed to have certain contraband.</param>
    /// <param name="contrabandList">All contraband prototypes present in storage.</param>
    public bool ContainerHasContraband(Entity<ContainerManagerComponent?> contraband, EntityUid? player, out List<ProtoId<ContrabandSeverityPrototype>> contrabandList)
    {
        contrabandList = [];

        if (!Resolve(contraband.Owner, ref contraband.Comp, false))
            return false;

        foreach (var container in contraband.Comp.Containers.Values)
        {
            foreach (var ent in container.ContainedEntities)
            {
                if (IsContraband(ent, player, out var itemContraId))
                    contrabandList.Add((ProtoId<ContrabandSeverityPrototype>)itemContraId);

                ContainerHasContraband(ent, player, out var itemContraList);

                contrabandList = contrabandList.Concat(itemContraList).ToList();
            }
        }

        return contrabandList.Any();
    }
}

/// <summary>
/// The item type that the contraband text should follow in the description text.
/// </summary>
public enum ContrabandItemType
{
    Item,
    Reagent
}
