namespace Content.Shared.Body
{
    /// <summary>
    ///     Used to determine whether a BodyPart can connect to another BodyPart.
    /// </summary>
    public enum BodyPartCompatibility
    {
        Universal,
        Biological,
        Mechanical
    }

    /// <summary>
    ///     Each BodyPart has a BodyPartType used to determine a variety of things - for instance, what slots it can fit into.
    /// </summary>
    public enum BodyPartType
    {
        Other,
        Torso,
        Head,
        Arm,
        Hand,
        Leg,
        Foot
    }

    /// <summary>
    ///     Defines a surgery operation that can be performed.
    /// </summary>
    public enum SurgeryType
    {
        Incision,
        Retraction,
        Cauterization,
        VesselCompression,
        Drilling,
        Amputation
    }
}
