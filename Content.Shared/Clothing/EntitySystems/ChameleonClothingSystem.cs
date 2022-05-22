using Content.Shared.Clothing.Components;
using Content.Shared.Verbs;

namespace Content.Shared.Clothing.EntitySystems;

public sealed class ChameleonClothingSystem : EntitySystem
{
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

        appearance.SetData(ChameleonVisuals.ClothingId, "ClothingUniformJumpsuitClown");
    }
}
