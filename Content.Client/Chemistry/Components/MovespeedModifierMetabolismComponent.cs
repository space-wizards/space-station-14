using Content.Shared.Chemistry.Components;
using Robust.Shared.GameObjects;

namespace Content.Client.Chemistry.Components
{
    //TODO: refactor movement modifier component because this is a pretty poor solution
    [RegisterComponent]
    [ComponentReference(typeof(SharedMovespeedModifierMetabolismComponent))]
    public class MovespeedModifierMetabolismComponent : SharedMovespeedModifierMetabolismComponent
    {

    }
}
