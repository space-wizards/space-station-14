using Content.Server.Health.BodySystem.Surgery.SurgeryData;

namespace Content.Server.Health.BodySystem
{

    /// <summary>
    ///     Making a class inherit from this interface allows you to do many things with it in the <see cref="ISurgeryData"/> class. This includes passing
    ///     it as an argument to a <see cref="ISurgeryData.SurgeryAction"/> delegate, as to later typecast it back to the original class type. Every BodyPart also needs an
    ///     IBodyPartContainer to be its parent (i.e. the BodyManagerComponent holds many BodyParts, each of which have an upward reference to it).
    /// </summary>

    public interface IBodyPartContainer
    {

    }
}
