using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Content.Shared.Store;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Shared._Starlight.Abstract.Conditions;

public sealed partial class SubjectMindAntagCondition : BaseCondition
{
    [DataField]
    public HashSet<ProtoId<AntagPrototype>>? Whitelist;
    [DataField]
    public HashSet<ProtoId<AntagPrototype>>? Blacklist;

    public override bool Handle(EntityUid @subject, EntityUid @object)
    {
        base.Handle(@subject, @object);

        var mind = Ent.System<SharedMindSystem>().GetMind(@subject);
        if (mind == null)
            return false;

        var roles = Ent.System<SharedRoleSystem>().MindGetAllRoleInfo((mind.Value, null));

        if (Blacklist != null)
        {
            foreach (var role in roles)
            {
                if (!role.Antagonist || string.IsNullOrEmpty(role.Prototype))
                    continue;

                if (Blacklist.Contains(role.Prototype))
                    return false;
            }
        }

        if (Whitelist != null)
        {
            var found = false;
            foreach (var role in roles)
            {

                if (!role.Antagonist || string.IsNullOrEmpty(role.Prototype))
                    continue;

                if (Whitelist.Contains(role.Prototype))
                    found = true;
            }
            if (!found)
                return false;
        }

        return true;
    }
}
