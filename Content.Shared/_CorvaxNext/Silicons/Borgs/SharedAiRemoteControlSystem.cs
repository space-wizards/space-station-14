using Content.Shared._CorvaxNext.Silicons.Borgs.Components;
using Content.Shared.Actions;
using Content.Shared.Mind;
using Content.Shared.Silicons.StationAi;
using Robust.Shared.Serialization;

namespace Content.Shared._CorvaxNext.Silicons.Borgs;

public abstract class SharedAiRemoteControlSystem : EntitySystem
{
    [Dependency] private readonly SharedStationAiSystem _stationAiSystem = default!;
    [Dependency] private readonly SharedTransformSystem _xformSystem = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    public void ReturnMindIntoAi(EntityUid entity)
    {
        if (!TryComp<AiRemoteControllerComponent>(entity, out var remoteComp))
            return;

        if (remoteComp?.AiHolder == null || !_stationAiSystem.TryGetCore(remoteComp.AiHolder.Value, out var stationAiCore) || stationAiCore.Comp?.RemoteEntity == null)
            return;

        if (remoteComp.LinkedMind == null)
            return;

        if (!TryComp<StationAiHeldComponent>(remoteComp.AiHolder.Value, out var stationAiHeldComp))
            return;

        stationAiHeldComp.CurrentConnectedEntity = null;

        _mind.TransferTo(remoteComp.LinkedMind.Value, remoteComp.AiHolder);

        _stationAiSystem.SwitchRemoteEntityMode(stationAiCore, true);
        remoteComp.AiHolder = null;
        remoteComp.LinkedMind = null;

        _xformSystem.SetCoordinates(stationAiCore.Comp.RemoteEntity.Value, Transform(entity).Coordinates);
    }
}

public sealed partial class ReturnMindIntoAiEvent : InstantActionEvent
{

}

public sealed partial class ToggleRemoteDevicesScreenEvent : InstantActionEvent
{

}

[Serializable, NetSerializable]
public enum RemoteDeviceUiKey : byte
{
    Key
}