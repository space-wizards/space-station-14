using Content.Shared.Atmos.Components;

namespace Content.Shared.Atmos.Consoles;

public abstract partial class SharedAtmosAlertsComputerSystem : EntitySystem
{
    public static Dictionary<Gas, string> GasShorthands = new Dictionary<Gas, string>()
    {
        [Gas.Ammonia] = "NH₃",
        [Gas.CarbonDioxide] = "CO₂",
        [Gas.Frezon] = "F",
        [Gas.Nitrogen] = "N₂",
        [Gas.NitrousOxide] = "N₂O",
        [Gas.Oxygen] = "O₂",
        [Gas.Plasma] = "P",
        [Gas.Tritium] = "T",
        [Gas.WaterVapor] = "H₂O",
    };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AtmosAlertsComputerComponent, AtmosAlertsComputerDeviceSilencedMessage>(OnDeviceSilencedMessage);
    }

    private void OnDeviceSilencedMessage(EntityUid uid, AtmosAlertsComputerComponent component, AtmosAlertsComputerDeviceSilencedMessage args)
    {
        if (args.SilenceDevice)
            component.SilencedDevices.Add(args.AtmosDevice);

        else
            component.SilencedDevices.Remove(args.AtmosDevice);

        Dirty(uid, component);
    }
}
