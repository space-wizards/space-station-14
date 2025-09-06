namespace Content.Shared.Temperature;

/// <summary>
/// Interface for all entities/components working with a <see cref="HeatContainer"/>.
/// </summary>
public interface IHeatContainerHolder
{
    HeatContainer HeatContainer { get; set; }
}
