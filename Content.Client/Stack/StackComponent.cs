using Content.Client.Items.Components;
using Content.Client.Message;
using Content.Client.Stylesheets;
using Content.Shared.Stacks;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client.Stack
{
    [RegisterComponent, Access(typeof(StackSystem), typeof(StackStatusControl))]
    [ComponentReference(typeof(SharedStackComponent))]
    public sealed class StackComponent : SharedStackComponent
    {
        [ViewVariables]
        public bool UiUpdateNeeded { get; set; }
    }
}
