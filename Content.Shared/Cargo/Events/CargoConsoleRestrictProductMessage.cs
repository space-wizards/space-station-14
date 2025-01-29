using Robust.Shared.Serialization;

namespace Content.Shared.Cargo.Events;

[Serializable, NetSerializable]
public sealed class CargoConsoleRestrictProductMessage(string product) : BoundUserInterfaceMessage
{
    public string Product { get; set; } = product;
}
