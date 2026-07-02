using Content.Server.NodeContainer.EntitySystems;
using Content.Server.Power.Generator;
using Content.Server.Popups;
using Content.Server.Power.Nodes;
using Content.Shared.NodeContainer;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Timing;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Power.EntitySystems;

public sealed partial class VoltageTogglerSystem : SharedVoltageTogglerSystem
{
    [Dependency] private UseDelaySystem _useDelay = null!;
    [Dependency] private PopupSystem _popup = null!;
    [Dependency] private SharedAudioSystem _audio = null!;
    [Dependency] private NodeGroupSystem _nodeGroupSystem = null!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<VoltageTogglerComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<VoltageTogglerComponent> entity, ref MapInitEvent args)
    {
        ChangeVoltage(entity, entity.Comp.SelectedVoltageLevel, null);
    }

    /// <summary>
    /// This changes the voltage to the next one in the voltage settings list.
    /// Or the first one if the current setting is the last.
    /// </summary>
    /// <remarks>This is used by <see cref="PortableGeneratorSystem"/></remarks>
    public void Cycle(Entity<VoltageTogglerComponent?> entity, EntityUid? user)
    {
        if (!Resolve(entity.Owner, ref entity.Comp))
            return;

        var nextVoltageLevel = (entity.Comp.SelectedVoltageLevel + 1) % entity.Comp.Settings.Length;
        ChangeVoltage((entity, entity.Comp), nextVoltageLevel, user);
    }

    protected override void ChangeVoltage(Entity<VoltageTogglerComponent> entity, int settingIndex, EntityUid? user)
    {
        if (settingIndex < 0 || settingIndex >= entity.Comp.Settings.Length)
            throw new IndexOutOfRangeException();

        if (TryComp<UseDelayComponent>(entity, out var useDelay) && _useDelay.IsDelayed((entity, useDelay)))
            return;

        if (!TryComp<NodeContainerComponent>(entity, out var nodeContainer))
            return;

        entity.Comp.SelectedVoltageLevel = settingIndex;
        var setting = entity.Comp.Settings[settingIndex];

        foreach (var node in nodeContainer.Nodes)
        {
            var cableNode = (CableDeviceNode)node.Value;
            cableNode.Enabled = setting.Node == node.Key;
            _nodeGroupSystem.QueueReflood(cableNode);
        }

        Dirty(entity);

        var ev = new VoltageChangeEvent(setting);
        RaiseLocalEvent(entity, ref ev);

        if (useDelay != null)
            _useDelay.TryResetDelay((entity, useDelay));

        if (user == null)
            return;

        if (entity.Comp.SwitchText != null)
        {
            var voltage = setting.Voltage;
            var popup = Loc.GetString(entity.Comp.SwitchText, ("voltage", VoltageString(voltage)));
            _popup.PopupEntity(popup, entity, user.Value);
        }

        _audio.PlayPvs(entity.Comp.SwitchSound, entity);
    }
}
