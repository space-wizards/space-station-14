using Content.Shared.Item;
using Content.Shared.Polymorph;
using Content.Shared.Polymorph.Components;
using Content.Shared.Polymorph.Systems;

namespace Content.Server.Polymorph.Systems;

public sealed class ChameleonProjectorSystem : SharedChameleonProjectorSystem
{
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChameleonDisguiseComponent, GotEquippedHandEvent>(OnEquippedHand);
    }

    private void OnEquippedHand(Entity<ChameleonDisguiseComponent> ent, ref GotEquippedHandEvent args)
    {
        _polymorph.Revert(ent.Owner);
        args.Cancel();
    }

    public override void Disguise(PolymorphConfiguration config, EntityUid user, EntityUid entity)
    {
        if (_polymorph.PolymorphEntity(user, config) is not {} disguise)
            return;

        var meta = MetaData(entity);
        _meta.SetEntityName(disguise, meta.EntityName);
        _meta.SetEntityDescription(disguise, meta.EntityDescription);

        var comp = EnsureComp<ChameleonDisguiseComponent>(disguise);
        comp.SourceEntity = entity;
        comp.SourceProto = Prototype(entity)?.ID;
        Dirty(disguise, comp);
    }
}
