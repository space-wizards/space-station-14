using Content.Server.Collections;
using Content.Shared.Collections;
using Content.Shared.Power.Pow3r.Nodes;

namespace Content.Server.Power.Pow3r.Nodes;

public sealed class PowerLoadProvider : IPowerLoad
{
    public PowerLoadProvider(GenIdStorage<PowerLoad> storage)
    {
        Storage = storage;
    }

    public NodeId Id { get; set; }

    public GenIdStorage<PowerLoad> Storage;

    public NodeId LinkedNetwork { get; set; }

    public bool Enabled
    {
        get => Storage[Id].Enabled;
        set => Storage[Id].Enabled = value;
    }

    public bool Paused
    {
        get => Storage[Id].Paused;
        set => Storage[Id].Paused = value;
    }

    public float DesiredPower
    {
        get => Storage[Id].DesiredPower;
        set => Storage[Id].DesiredPower = value;
    }

    public float ReceivingPower
    {
        get => Storage[Id].ReceivingPower;
        set => Storage[Id].ReceivingPower = value;
    }
}
