namespace Content.Shared.Administration.Logs.Payloads;

/// <summary>
/// Marker interface for top-level payload records that carry a schema version field.
/// Every named payload record that is serialized into the admin-log or audit-log
/// JSON column must implement this interface.
/// </summary>
/// <remarks>
/// The <c>SchemaVersion</c> property is serialized by <c>System.Text.Json</c> via <c>_jsonOptions</c>, so it appears as <c>"schemaVersion": N</c>
/// in stored JSON. V1 is the initial value for all existing payload types.
/// Increment the version when a breaking field change is made to the payload contract.
/// </remarks>
public interface IVersionedPayload
{
    /// <summary>Schema version of this payload record. Start at 1; increment on breaking changes.</summary>
    int SchemaVersion { get; }
}
