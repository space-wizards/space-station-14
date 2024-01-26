using Content.Server.Clothing.Components;
using Content.Server.Hands.Systems;
using Content.Shared.Clothing;
using Content.Shared.DoAfter;
using Content.Shared.Inventory;
using Content.Shared.Verbs;
using Robust.Server.Containers;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Utility;

namespace Content.Server.Clothing.Systems;

public sealed class TransformableClothingSystem : EntitySystem
{
    [Dependency] private readonly HandsSystem _handsSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly ContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TransformableClothingComponent, GetVerbsEvent<AlternativeVerb>>(OnAltVerb);
        SubscribeLocalEvent<TransformableClothingComponent, TransformableClothingDoAfterEvent>(OnDoAfter);
    }

    private void OnAltVerb(EntityUid uid, TransformableClothingComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess || args.Hands == null)
            return;

        // don't allow transforming if equipped
        if (_inventorySystem.TryGetContainingSlot((uid), out var slotDef))
            return;

        args.Verbs.Add(new AlternativeVerb
        {
            Priority = 100,
            Act = () => {
                _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, component.TransformDelay, new TransformableClothingDoAfterEvent(), uid, target: args.Target)
                {
                    BreakOnTargetMove = true,
                    NeedHand = true
                });
            },
            Text = Loc.GetString("transform-clothing-verb-text"),
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/refresh.svg.192dpi.png"))
        });
    }

    private void OnDoAfter(EntityUid uid, TransformableClothingComponent component, DoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (args.Target == null)
            return;

        TransformClothing(uid, args.User, component);
    }

    private void TransformClothing(EntityUid uid, EntityUid user, TransformableClothingComponent component)
    {
        var xform = Transform(uid);
        var transformedEnt = EntityManager.SpawnEntity(component.TransformProto, xform.Coordinates);

        if (_handsSystem.IsHolding(user, uid, out var hand))
        {
            _handsSystem.TryDrop(user, uid);
            _handsSystem.TryPickupAnyHand(user, transformedEnt);
        }
        else if(_containerSystem.TryGetContainingContainer(uid, out var container))
        {
            _containerSystem.Remove(uid, container);
            _containerSystem.Insert(transformedEnt, container);
        }

        QueueDel(uid);

        _audioSystem.PlayPvs(component.TransformSound, transformedEnt);
    }
}
