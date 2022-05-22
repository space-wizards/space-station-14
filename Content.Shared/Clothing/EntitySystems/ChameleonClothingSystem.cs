using Content.Shared.Clothing.Components;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;

namespace Content.Shared.Clothing.EntitySystems;

public sealed class ChameleonClothingSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ChameleonClothingComponent, GetVerbsEvent<InteractionVerb>>(OnVerb);
    }

    private void OnVerb(EntityUid uid, ChameleonClothingComponent component, GetVerbsEvent<InteractionVerb> args)
    {
        args.Verbs.Add(new InteractionVerb()
        {
            Text = "Cham",
            Act = () => Cham(uid, component)
        });
    }

    public void Cham(EntityUid uid, ChameleonClothingComponent? component = null, AppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref appearance, ref component))
            return;

        var protoId = "ClothingUniformJumpskirtResearchDirector";
        if (!_proto.TryIndex(protoId, out EntityPrototype? proto))
            return;

        // copy name and description
        var meta = MetaData(uid);
        meta.EntityName = proto.Name;
        meta.EntityDescription = proto.Description;

        appearance.SetData(ChameleonVisuals.ClothingId, protoId);
    }
}
