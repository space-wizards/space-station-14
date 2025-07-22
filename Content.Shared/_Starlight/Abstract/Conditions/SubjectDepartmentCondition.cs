using System.Linq;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Mind;
using Content.Shared.PDA;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Content.Shared.Store;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Shared._Starlight.Abstract.Conditions;

public sealed partial class SubjectDepartmentCondition : BaseCondition
{
    [Dependency] public readonly IPrototypeManager PrototypeManager = default!;
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;

    [DataField]
    public HashSet<ProtoId<DepartmentPrototype>>? Whitelist;

    [DataField]
    public HashSet<ProtoId<DepartmentPrototype>>? Blacklist;

    public override bool Handle(EntityUid @subject, EntityUid @object)
    {
        base.Handle(@subject, @object);

        if (!_accessReader.FindAccessItemsInventory(@subject, out var items))
            return false;

        List<ProtoId<DepartmentPrototype>>? departments = null;

        foreach (var item in items)
        {
            // ID Card
            if (Ent.TryGetComponent<IdCardComponent>(item, out var id))
            {
                departments = id.JobDepartments;
                break;
            }

            // PDA
            if (Ent.TryGetComponent<PdaComponent>(item, out var pda)
                && pda.ContainedId != null
                && Ent.TryGetComponent(pda.ContainedId, out id))
            {
                departments = id.JobDepartments;
                break;
            }
        }

        return departments != null
            && (Whitelist == null || departments.All(Whitelist.Contains))
            && (Blacklist == null || !departments.Any(Blacklist.Contains));
    }
}
