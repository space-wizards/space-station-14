using Content.Shared.Inventory;

namespace Content.Shared.Temperature;

public sealed class ModifyChangedTemperatureEvent : EntityEventArgs, IInventoryRelayEvent
{
    public SlotFlags TargetSlots { get; } = ~SlotFlags.POCKET;

    public float HeatDelta;

    public ModifyChangedTemperatureEvent(float heat)
    {
        HeatDelta = heat;
    }
}

public sealed class OnTemperatureChangeEvent : EntityEventArgs
{
    public readonly float CurrentTemperature;
    public readonly float LastTemperature;
    public readonly float HeatDelta;

    public OnTemperatureChangeEvent(float current, float last, float heatDelta)
    {
        CurrentTemperature = current;
        LastTemperature = last;
        HeatDelta = heatDelta;
    }
}

