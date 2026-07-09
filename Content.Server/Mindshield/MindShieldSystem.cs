using Content.Server.Administration.Logs;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Server.Roles;
using Content.Shared.Database;
using Content.Shared.Implants;
using Content.Shared.Mindshield.Components;
using Content.Shared.Revolutionary;
using Content.Shared.Revolutionary.Components;
using Content.Shared.Roles.Components;
using Robust.Shared.Containers;
using Content.Shared.Antag;
using Content.Shared.Overlays;
using Robust.Shared.Player;
using Robust.Shared.GameStates;

namespace Content.Server.Mindshield;

/// <summary>
/// System used for adding or removing components with a mindshield implant
/// as well as checking if the implanted is a Rev or Head Rev.
/// </summary>
public sealed partial class MindShieldSystem : EntitySystem
{
    [Dependency] private IAdminLogManager _adminLogManager = default!;
    [Dependency] private RoleSystem _roleSystem = default!;
    [Dependency] private MindSystem _mindSystem = default!;
    [Dependency] private PopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MindShieldImplantComponent, ImplantImplantedEvent>(OnImplantImplanted);
        SubscribeLocalEvent<MindShieldImplantComponent, ImplantRemovedEvent>(OnImplantRemoved);
        SubscribeLocalEvent<MindShieldComponent, AttemptConvertRevolutionaryEvent>(OnAttemptConvert);
        SubscribeLocalEvent<MindShieldComponent, ComponentGetStateAttemptEvent>(OnMindShieldGetStateAttempt);
        SubscribeLocalEvent<ShowMindShieldIconsComponent, ComponentStartup>(DirtyMindShieldComps);
        // Also dirty when ShowAntagIconsComponent is gained (e.g. admin/ghost mid-round)
        // so the new observer sees existing mindshield icons immediately.
        SubscribeLocalEvent<ShowAntagIconsComponent, ComponentStartup>(DirtyMindShieldComps);
    }

    private void OnImplantImplanted(Entity<MindShieldImplantComponent> ent, ref ImplantImplantedEvent ev)
    {
        EnsureComp<MindShieldComponent>(ev.Implanted);
        MindShieldRemovalCheck(ev.Implanted, ev.Implant);
    }

    /// <summary>
    /// Checks if the implanted person was a Rev or Head Rev and remove role or destroy mindshield respectively.
    /// </summary>
    private void MindShieldRemovalCheck(EntityUid implanted, EntityUid implant)
    {
        if (HasComp<HeadRevolutionaryComponent>(implanted))
        {
            _popupSystem.PopupEntity(Loc.GetString("head-rev-break-mindshield"), implanted);
            QueueDel(implant);
            return;
        }

        if (_mindSystem.TryGetMind(implanted, out var mindId, out _) &&
            _roleSystem.MindRemoveRole<RevolutionaryRoleComponent>(mindId))
        {
            _adminLogManager.Add(LogType.Mind, LogImpact.Medium, $"{ToPrettyString(implanted)} was deconverted due to being implanted with a Mindshield.");
        }
    }

    private void OnImplantRemoved(Entity<MindShieldImplantComponent> ent, ref ImplantRemovedEvent args)
    {
        RemComp<MindShieldComponent>(args.Implanted);
    }

    private void OnAttemptConvert(Entity<MindShieldComponent> ent, ref AttemptConvertRevolutionaryEvent args)
    {
        args.Cancelled = true;
    }

    private void OnMindShieldGetStateAttempt(EntityUid uid, MindShieldComponent comp, ref ComponentGetStateAttemptEvent args)
    {
        args.Cancelled = !CanGetState(uid, args.Player);
    }

    private bool CanGetState(EntityUid target, ICommonSession? player)
    {
        if (player?.AttachedEntity is not {} user)
            return true;

        if (user == target)
            return true;

        if (HasComp<ShowAntagIconsComponent>(user))
            return true;

        if (HasComp<ShowMindShieldIconsComponent>(user))
            return true;

        return false;
    }

    private void DirtyMindShieldComps<T>(EntityUid someUid, T someComp, ComponentStartup ev)
    {
        var query = EntityQueryEnumerator<MindShieldComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            Dirty(uid, comp);
        }
    }
}

