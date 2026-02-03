using Content.Server.Power.Components;
using Content.Shared.NodeContainer;
using Content.Shared.NodeContainer.NodeGroups;
using Content.Shared.Power;
using Content.Shared.Verbs;

namespace Content.Server.Power.EntitySystems;

public sealed class VoltageTogglerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<VoltageTogglerComponent, GetVerbsEvent<Verb>>(OnGetVerb);
    }

    private void OnGetVerb(Entity<VoltageTogglerComponent> entity, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var index = 0;
        foreach (var setting in entity.Comp.Settings)
        {
            // This is because Act wont work with index.
            // Needs it to be saved in the loop.
            var currIndex = index;
            var verb = new Verb
            {
                Priority = currIndex,
                Category = VerbCategory.VoltageLevel,
                Disabled = entity.Comp.SelectedVoltageLevel == currIndex,
                Text = Loc.GetString(setting.Name),
                Act = () =>
                {
                    entity.Comp.SelectedVoltageLevel = currIndex;
                    Dirty(entity);

                    ChangeVoltage(entity, setting);
                }
            };
            args.Verbs.Add(verb);
            index++;
        }
    }

    private void ChangeVoltage(Entity<VoltageTogglerComponent> entity, VoltageSetting setting)
    {
        if (!TryComp<NodeContainerComponent>(entity, out var nodeContainerComp))
            return;

        if (!TryComp<PowerConsumerComponent>(entity, out var powerConsumerComp))
            return;

        var newNodeGroupId = NodeGroupID.Apc;

        switch (setting.Voltage)
        {
            case Voltage.Apc:
                newNodeGroupId = NodeGroupID.Apc;
                break;
            case Voltage.Medium:
                newNodeGroupId = NodeGroupID.MVPower;
                break;
            case Voltage.High:
                newNodeGroupId = NodeGroupID.HVPower;
                break;
        }

        nodeContainerComp.Nodes["input"].SetNodeGroupId(newNodeGroupId);

        powerConsumerComp.Voltage = setting.Voltage;
        powerConsumerComp.SetDrawRate(setting.Wattage);
    }
}
