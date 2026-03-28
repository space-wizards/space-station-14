using Robust.Shared.Serialization;

namespace Content.Shared.RussStation.Surgery;

/// <summary>
/// Sent server -> client to open the surgery procedure selection menu.
/// </summary>
[Serializable, NetSerializable]
public sealed class OpenSurgeryMenuEvent : EntityEventArgs
{
    public NetEntity Target;
    public NetEntity Bedsheet;
    public List<string> ProcedureIds;

    public OpenSurgeryMenuEvent(NetEntity target, NetEntity bedsheet, List<string> procedureIds)
    {
        Target = target;
        Bedsheet = bedsheet;
        ProcedureIds = procedureIds;
    }
}

/// <summary>
/// Sent client -> server when a procedure is selected from the menu.
/// </summary>
[Serializable, NetSerializable]
public sealed class SelectSurgeryProcedureEvent : EntityEventArgs
{
    public NetEntity Target;
    public NetEntity Bedsheet;
    public string ProcedureId;

    public SelectSurgeryProcedureEvent(NetEntity target, NetEntity bedsheet, string procedureId)
    {
        Target = target;
        Bedsheet = bedsheet;
        ProcedureId = procedureId;
    }
}

/// <summary>
/// Sent server -> client to open organ selection sub-menu.
/// </summary>
[Serializable, NetSerializable]
public sealed class OpenOrganMenuEvent : EntityEventArgs
{
    public NetEntity Target;
    public List<(NetEntity OrganId, string Name, string? ProtoId)> Organs;

    public OpenOrganMenuEvent(NetEntity target, List<(NetEntity OrganId, string Name, string? ProtoId)> organs)
    {
        Target = target;
        Organs = organs;
    }
}

/// <summary>
/// Sent client -> server when an organ is selected for removal.
/// </summary>
[Serializable, NetSerializable]
public sealed class SelectOrganEvent : EntityEventArgs
{
    public NetEntity Target;
    public NetEntity OrganId;

    public SelectOrganEvent(NetEntity target, NetEntity organId)
    {
        Target = target;
        OrganId = organId;
    }
}
