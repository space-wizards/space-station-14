using System.Linq;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Emag.Components;
using Content.Shared.Mind;
using Robust.Shared.Prototypes;

namespace Content.Shared._Starlight.Abstract.Conditions;

public sealed partial class SubjectAccessCondition : BaseCondition
{
    [Dependency] public readonly IPrototypeManager PrototypeManager = default!;

    [DataField(required: true)]
    public ProtoId<AccessLevelPrototype> access = default!;

    public override bool Handle(EntityUid @subject, EntityUid @object)
    {
        base.Handle(@subject, @object);
        return !Ent.TryGetComponent<AccessReaderComponent>(@object, out var accessReader)
                || Ent.System<AccessReaderSystem>().IsAllowed(@subject, @object, accessReader)
                || Ent.HasComponent<EmaggedComponent>(@object);
    }
}