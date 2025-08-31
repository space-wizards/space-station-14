using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Emag.Components;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Mind;
using Content.Shared.Store;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Shared._Starlight.Abstract.Conditions;

public sealed partial class SubjectSpeciesCondition : BaseCondition
{
    [DataField]
    public HashSet<ProtoId<SpeciesPrototype>>? Whitelist;

    [DataField]
    public HashSet<ProtoId<SpeciesPrototype>>? Blacklist;
    public override bool Handle(EntityUid @subject, EntityUid @object)
    {
        base.Handle(@subject, @object);
        return Ent.TryGetComponent<HumanoidAppearanceComponent>(@subject, out var appearance)
            && (Blacklist == null || !Blacklist.Contains(appearance.Species))
            && (Whitelist == null || Whitelist.Contains(appearance.Species));
    }
}
