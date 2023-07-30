namespace Content.Server.Power.Generation.Teg;

public sealed class TegSensorData
{
    public Circulator CirculatorA;
    public Circulator CirculatorB;

    public record struct Circulator(
        float InletPressure,
        float OutletPressure,
        float InletTemperature,
        float OutletTemperature);
}

