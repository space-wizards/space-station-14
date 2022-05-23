using Content.Shared.Clothing.Components;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server.Clothing.Systems;

public sealed class ChameleonClothingSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ChameleonClothingComponent, GetVerbsEvent<InteractionVerb>>(OnVerb);
    }

    private void OnVerb(EntityUid uid, ChameleonClothingComponent component, GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        args.Verbs.Add(new InteractionVerb()
        {
            Text = "Cham",
            Act = () => TryOpenUi(uid, args.User, component)
        });
    }

    private void TryOpenUi(EntityUid uid, EntityUid user, ChameleonClothingComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;
        if (!TryComp(user, out ActorComponent? actor))
            return;
        _uiSystem.TryToggleUi(uid, ChameleonUiKey.Key, actor.PlayerSession);
    }

    /// <summary>
    ///     Change chameleon items name, description and sprite to mimic other entity prototype
    /// </summary>
    public void MimicPrototype(EntityUid uid, string protoId, ChameleonClothingComponent? component = null, AppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref appearance, ref component))
            return;
        if (!_proto.TryIndex(protoId, out EntityPrototype? proto))
            return;

        // copy name and description
        var meta = MetaData(uid);
        meta.EntityName = proto.Name;
        meta.EntityDescription = proto.Description;

        // world, in hand and clothing sprite will be set by visualizer
        appearance.SetData(ChameleonVisuals.ClothingId, protoId);
    }
}
