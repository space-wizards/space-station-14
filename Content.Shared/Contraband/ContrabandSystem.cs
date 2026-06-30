using System.Diagnostics.CodeAnalysis;
using Content.Shared.Access.Systems;
using Content.Shared.CCVar;
using Content.Shared.Examine;
using Content.Shared.Localizations;
using Content.Shared.Roles;
using Content.Shared.Verbs;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Shared.Contraband;

/// <summary>
/// This handles showing examine messages for contraband-marked items.
/// </summary>
public sealed partial class ContrabandSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _configuration = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SharedIdCardSystem _id = default!;
    [Dependency] private ExamineSystemShared _examine = default!;

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

        // Checking if contraband is only shown in the HUD
        if (_contrabandExamineOnlyInHudEnabled)
        {
            var ev = new GetContrabandDetailsEvent();
            RaiseLocalEvent(args.User, ref ev);
            if (!ev.CanShowContraband)
                return;
        }

        // CanAccess is not used here, because we want people to be able to examine legality in strip menu.
        if (!args.CanInteract)
            return;

        // two strings:
        // one, the actual informative 'this is restricted'
        // then, the 'you can/shouldn't carry this around' based on the ID the user is wearing
        var severity = _proto.Index(component.Severity);
        String departmentExamineMessage;
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
        var carryingMessage = Loc.GetString("contraband-examine-text-in-the-clear");
        var iconTexture = "/Textures/Interface/VerbIcons/unlock-green.svg.192dpi.png";
        if (IsContraband((ent, component), args.User, out _))
        {
            carryingMessage = Loc.GetString("contraband-examine-text-avoid-carrying-around");
            iconTexture = "/Textures/Interface/VerbIcons/lock-red.svg.192dpi.png";
        }
        var examineMarkup = GetContrabandExamine(departmentExamineMessage, carryingMessage);
        _examine.AddHoverExamineVerb(args,
            component,
            Loc.GetString("contraband-examinable-verb-text"),
            examineMarkup.ToMarkup(),
            iconTexture);
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
        var localizedDepartments = allowedDepartments.Select(p => Loc.GetString("contraband-department-plural", ("department", Loc.GetString(_proto.Index(p).Name))));
        var jobs = allowedJobs.Select(p => _proto.Index(p).LocalizedName).ToArray();
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

        var jobs = contraband.Comp.AllowedJobs.Select(p => _proto.Index(p).LocalizedName).ToArray();
        // if it is fully restricted, you're department-less, or your department isn't in the allowed list, you cannot carry it. Otherwise, you can.
        if (departments.Intersect(contraband.Comp.AllowedDepartments).Any() || jobs.Contains(jobId))
            return false;

        return true;
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
