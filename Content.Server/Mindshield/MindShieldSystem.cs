using Content.Shared.Mindshield.Components;
using Content.Shared.Revolutionary.Components;
using Content.Server.Popups;
using Content.Shared.Database;
using Content.Server.Administration.Logs;
using Content.Shared.Stunnable;
using Content.Server.Mind;
using Robust.Shared.Prototypes;
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
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly RoleSystem _roleSystem = default!;
    [Dependency] private readonly SharedStunSystem _sharedStun = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SubdermalImplantComponent, ImplantCheckEvent>(ImplantCheck);
        SubscribeLocalEvent<MindShieldComponent, ComponentAdd>(MindShieldAdded);
    }

    public void ImplantCheck(EntityUid uid, SubdermalImplantComponent comp, ref ImplantCheckEvent ev)
    {
        if (_tag.HasTag(ev.Implant, "MindShield") && ev.Implanted != null)
        {
            EnsureComp<MindShieldComponent>((EntityUid) ev.Implanted);
        }
    }


    /// <summary>
    /// When the MindShield is added this will trigger to check if the implanted is a Rev and remove their antag role.
    /// </summary>
    private void MindShieldAdded(EntityUid uid, MindShieldComponent comp, ComponentAdd init)
    {
        if (HasComp<RevolutionaryComponent>(uid) && !HasComp<HeadRevolutionaryComponent>(uid))
        {
            _mindSystem.TryGetMind(uid, out var mindId, out var mind);
            _adminLogManager.Add(LogType.Mind, LogImpact.Medium, $"{ToPrettyString(uid)} was deconverted due to being implanted with a Mindshield.");
            _roleSystem.MindTryRemoveRole<RevolutionaryRoleComponent>(mindId);
        }
    }
}
