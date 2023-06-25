namespace Content.Shared.Cargo.Orders;

public readonly record struct CargoOrderStringRepresentation(int OrderId, string ProductName, EntityUid? EntityUid, int Price, int Amount, string Requester, string Reason, string? Approver) : IFormattable
{
    public override string ToString()
    {
        if (EntityUid != null)
            return $"CargoOrder[{OrderId}: {ProductName}]";
        return $"CargoOrder[{OrderId}: {Amount} of {ProductName}]";
    }

    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        return ToString();
    }

    public static implicit operator string(CargoOrderStringRepresentation rep) => rep.ToString();
}
