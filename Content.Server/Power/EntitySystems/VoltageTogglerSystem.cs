using Content.Server.NodeContainer.EntitySystems;
using Content.Server.Power.Components;
using Content.Server.Power.Nodes;
using Content.Shared.NodeContainer;
using Content.Shared.NodeContainer.NodeGroups;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Power;
using Content.Shared.Verbs;

namespace Content.Server.Power.EntitySystems;

public sealed class VoltageTogglerSystem : SharedVoltageTogglerSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<VoltageTogglerComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<VoltageTogglerComponent> entity, ref MapInitEvent args)
    {
        var startSetting = entity.Comp.Settings[entity.Comp.SelectedVoltageLevel];
        var ev = new VoltageChangedEvent(startSetting);

        RaiseLocalEvent(entity, ref ev);
    }
}
