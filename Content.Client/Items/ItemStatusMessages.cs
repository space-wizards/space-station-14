using Robust.Client.UserInterface;

namespace Content.Client.Items
{
    public sealed class ItemStatusCollectMessage : EntityEventArgs
    {
        public List<Control> Controls = new();
    }

    public static class ItemStatusRegisterExt
    {
        /// <summary>
        /// Register an item status control for a component.
        /// </summary>
        /// <param name="subs">The <see cref="EntitySystem.Subs"/> handle from within entity system initialize.</param>
        /// <param name="createControl">A delegate to create the actual control.</param>
        /// <typeparam name="TComp">The type of component for which this control should be made.</typeparam>
        public static void ItemStatus<TComp>(
            this EntitySystem.Subscriptions subs,
            Func<EntityUid, Control> createControl)
            where TComp : IComponent
        {
            subs.SubscribeLocalEvent<TComp, ItemStatusCollectMessage>((uid, _, args) =>
            {
                args.Controls.Add(createControl(uid));
            });
        }
    }
}
