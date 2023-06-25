using System.Text.Json;
using Content.Shared.Cargo.Orders;

namespace Content.Server.Administration.Logs.Converters;

[AdminLogConverter]
public sealed class CargoOrderDataStringRepresentationConverter : AdminLogConverter<CargoOrderStringRepresentation>
{
    public override void Write(Utf8JsonWriter writer, CargoOrderStringRepresentation value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WriteNumber("orderId", value.OrderId);
        writer.WriteString("productName", value.ProductName);
        if (value.EntityUid != null)
            writer.WriteNumber("entityUid", value.EntityUid.Value.GetHashCode()); // How do I make this use EntityUidConverter.cs instead?
        writer.WriteNumber("price", value.Price);
        writer.WriteNumber("amount", value.Amount);
        writer.WriteString("requester", value.Requester);
        writer.WriteString("reason", value.Reason);
        if (value.Approver != null)
            writer.WriteString("approver", value.Approver);

        writer.WriteEndObject();
    }
}
