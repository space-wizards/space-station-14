using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.UserInterface;
using Content.Shared.Verbs;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Shared._Starlight.Clothing;

public sealed partial class SharedRollUpSleevesSystem : EntitySystem
{
    [Dependency] private readonly ClothingSystem _clothingSystem = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RollUpSleevesComponent, GetVerbsEvent<Verb>>(GetVerb);
    }

    private void GetVerb(EntityUid uid, RollUpSleevesComponent component, GetVerbsEvent<Verb> args)
    {
        if (!args.CanInteract)
            return;

        args.Verbs.Add(new Verb
        {
            Act = () => RollUp(uid, component),
            Text = Loc.GetString("ui-verb-roll-up"),
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/rollup.png")),
        });
    }
    private void RollUp(EntityUid item, RollUpSleevesComponent component)
    {
        if (_net.IsClient || !TryComp<ClothingComponent>(item, out var clothing)) return;
        component.Rolled = !component.Rolled;
        _clothingSystem.SetEquippedPrefix(item, component.Rolled ? "rolled" : null, clothing);
    }
}
