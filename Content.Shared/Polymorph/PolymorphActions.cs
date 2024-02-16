using Content.Shared.Actions;

namespace Content.Shared.Polymorph;

public sealed partial class PolymorphActionEvent : InstantActionEvent
{
    /// <summary>
    /// The polymorph prototype containing all the information about
    /// the specific polymorph.
    /// </summary>
    public PolymorphPrototype Prototype = default!;

    public PolymorphActionEvent(PolymorphPrototype prototype) : this()
    {
        Prototype = prototype;
    }
}

public sealed partial class RevertPolymorphActionEvent : InstantActionEvent
{

}
