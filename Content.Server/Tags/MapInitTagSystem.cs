using Content.Server.Disposal.Tube.Components;
using Content.Server.Power.Components;
using Content.Shared.Item;
using Content.Shared.Tag;
using Content.Shared.Wall;
using Robust.Shared.GameObjects;

namespace Content.Server.Tag;
public sealed class MapInitTagSystem : EntitySystem
{
    [Dependency] private readonly TagSystem _tag = default!;

    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeLocalEvent<DisposalTubeComponent, MapInitEvent>(OnMapInitEvent);
        SubscribeLocalEvent<ItemComponent, MapInitEvent>(OnMapInitEvent);
        SubscribeLocalEvent<CableComponent, MapInitEvent>(OnMapInitEvent);
        SubscribeLocalEvent<WallMountComponent, MapInitEvent>(OnMapInitEvent);
        //SubscribeLocalEvent<YourComponent, MapInitEvent>(OnMapInitEvent);
    }

    private void OnMapInitEvent(EntityUid uid, Component component, MapInitEvent args)
    {
        switch (component)
        {
            case DisposalTubeComponent:
                _tag.AddTag(uid, "DisposalTube");
                break;
            case ItemComponent:
                _tag.AddTag(uid, "Item");
                break;
            case CableComponent:
                _tag.AddTag(uid, "Cable");
                break;
            case WallMountComponent:
                _tag.AddTag(uid, "WallMount");
                break;
            //case YourComponent:
                //_tag.AddTag(uid, "YourTag");
                //break;
        }
    }
}