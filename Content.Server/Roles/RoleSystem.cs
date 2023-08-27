using Content.Server.Roles.Jobs;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.Roles;

public sealed class RoleSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    // TODO please lord make role entities
    private readonly HashSet<Type> _antagTypes = new();

    public override void Initialize()
    {
        // TODO make roles entities
        SubscribeLocalEvent<JobComponent, MindGetAllRolesEvent>(OnJobGetAllRoles);

        SubscribeAntagEvents<NukeopsRoleComponent>();
        SubscribeAntagEvents<SubvertedSiliconRoleComponent>();
        SubscribeAntagEvents<TraitorRoleComponent>();
        SubscribeAntagEvents<ZombieRoleComponent>();
    }

    private void OnJobGetAllRoles(EntityUid uid, JobComponent component, ref MindGetAllRolesEvent args)
    {
        var name = "game-ticker-unknown-role";
        if (component.PrototypeId != null && _prototypes.TryIndex(component.PrototypeId, out JobPrototype? job))
        {
            name = job.Name;
        }

        name = Loc.GetString(name);

        args.Roles.Add(new RoleInfo(component, name, false));
    }

    private void SubscribeAntagEvents<T>() where T : AntagonistRoleComponent
    {
        SubscribeLocalEvent((EntityUid _, T component, ref MindGetAllRolesEvent args) =>
        {
            var name = "game-ticker-unknown-role";
            if (component.PrototypeId != null && _prototypes.TryIndex(component.PrototypeId, out AntagPrototype? antag))
            {
                name = antag.Name;
            }
            name = Loc.GetString(name);

            args.Roles.Add(new RoleInfo(component, name, true));
        });

        SubscribeLocalEvent((EntityUid _, T _, ref MindIsAntagonistEvent args) => args.IsAntagonist = true);
        _antagTypes.Add(typeof(T));
    }

    public List<RoleInfo> MindGetAllRoles(EntityUid mindId)
    {
        var ev = new MindGetAllRolesEvent(new List<RoleInfo>());
        RaiseLocalEvent(mindId, ref ev);
        return ev.Roles;
    }

    public bool MindIsAntagonist(EntityUid? mindId)
    {
        if (mindId == null)
            return false;

        var ev = new MindIsAntagonistEvent();
        RaiseLocalEvent(mindId.Value, ref ev);
        return ev.IsAntagonist;
    }

    public bool IsAntagonistRole<T>()
    {
        return _antagTypes.Contains(typeof(T));
    }
}
