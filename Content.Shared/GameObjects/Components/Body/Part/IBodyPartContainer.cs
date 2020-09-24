namespace Content.Shared.GameObjects.Components.Body.Part
{
    /// <summary>
    ///     Making a class inherit from this interface allows you to do many
    ///     things with it in the <see cref="SurgeryData"/> class.
    ///     This includes passing it as an argument to a
    ///     <see cref="SurgeryData.SurgeryAction"/> delegate, as to later typecast
    ///     it back to the original class type.
    ///     Every BodyPart also needs an <see cref="IBodyPartContainer"/> to be
    ///     its parent (i.e. the <see cref="IBody"/> holds many
    ///     <see cref="IBodyPart"/>s, each of which have an upward reference to it).
    /// </summary>
    // TODO BODY Remove
    public interface IBodyPartContainer
    {
    }
}
