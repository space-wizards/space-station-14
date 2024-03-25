using Content.Shared.Actions;
using Robust.Shared.Prototypes;

namespace Content.Shared.Polymorph;

public sealed partial class PolymorphActionEvent : InstantActionEvent
{
    /// <summary>
    /// The polymorph prototype containing all the information about
    /// the specific polymorph.
    /// </summary>
    public PolymorphPrototype? Prototype;

    /// <summary>
    ///     The polymorph proto id, used where individual polymorph actions need to be given through something like a shop
    /// </summary>
    [DataField]
    public ProtoId<PolymorphPrototype>? ProtoId;

    public PolymorphActionEvent(PolymorphPrototype prototype) : this()
    {
        Prototype = prototype;
    }
}

public sealed partial class RevertPolymorphActionEvent : InstantActionEvent
{

}
