using Content.Shared.Mindshield.Components;
using Content.Shared.Revolutionary.Components;
using Content.Server.Popups;
using Content.Shared.Database;
using Content.Server.Administration.Logs;
using Content.Server.Mind;
using Content.Shared.Implants;
using Content.Shared.Tag;
using Content.Server.Roles;
using Content.Shared.Implants.Components;

namespace Content.Server.Mindshield;

/// <summary>
/// System used for checking if the implanted is a Rev or Head Rev.
/// </summary>
public sealed class MindShieldSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogManager = default!;
    [Dependency] private readonly RoleSystem _roleSystem = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;

    [ValidatePrototypeId<TagPrototype>]
    public const string MindShieldTag = "MindShield";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SubdermalImplantComponent, ImplantImplantedEvent>(ImplantCheck);
    }

    /// <summary>
    /// Checks if the implant was a mindshield or not
    /// </summary>
    public void ImplantCheck(EntityUid uid, SubdermalImplantComponent comp, ref ImplantImplantedEvent ev)
    {
        if (_tag.HasTag(ev.Implant, MindShieldTag) && ev.Implanted != null)
        {
            EnsureComp<MindShieldComponent>(ev.Implanted.Value);
            MindShieldRemovalCheck(ev.Implanted, ev.Implant);
        }
    }

    /// <summary>
    /// Checks if the implanted person was a Rev or Head Rev and remove role or destroy mindshield respectively.
    /// </summary>
    public void MindShieldRemovalCheck(EntityUid? implanted, EntityUid implant)
    {
        if (HasComp<RevolutionaryComponent>(implanted) && !HasComp<HeadRevolutionaryComponent>(implanted))
        {
            _mindSystem.TryGetMind(implanted.Value, out var mindId, out _);
            _adminLogManager.Add(LogType.Mind, LogImpact.Medium, $"{ToPrettyString(implanted.Value)} was deconverted due to being implanted with a Mindshield.");
            _roleSystem.MindTryRemoveRole<RevolutionaryRoleComponent>(mindId);
        }
        else if (HasComp<RevolutionaryComponent>(implanted))
        {
            _popupSystem.PopupEntity(Loc.GetString("head-rev-break-mindshield"), implanted.Value);
            QueueDel(implant);
        }
    }
}
