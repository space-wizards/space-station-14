using Robust.Client.UserInterface;

namespace Content.Client.Items
{
    /// <summary>
    /// Raised by the HUD logic to collect item status controls for a held entity.
    /// </summary>
    /// <remarks>
    /// Handlers should add any controls they want to add to <see cref="Controls"/>.
    /// </remarks>
    /// <seealso cref="ItemStatusRegisterExt"/>
    public sealed class ItemStatusCollectMessage : EntityEventArgs
    {
        /// <summary>
        /// A list of controls that will be displayed on the HUD. Handlers should add their controls here.
        /// </summary>
        public List<Control> Controls = new();
    }

    /// <summary>
    /// Extension methods for registering item status controls.
    /// </summary>
    /// <seealso cref="ItemStatusCollectMessage"/>
    public static class ItemStatusRegisterExt
    {
        /// <summary>
        /// Register an item status control for a component.
        /// </summary>
        /// <remarks>
        /// This is a convenience wrapper around <see cref="ItemStatusCollectMessage"/>.
        /// </remarks>
        /// <param name="subs">The <see cref="EntitySystem.Subs"/> handle from within entity system initialize.</param>
        /// <param name="createControl">
        /// A delegate to create the actual control.
        /// If the delegate returns null, no control will be added to the item status.
        /// </param>
        /// <typeparam name="TComp">The type of component for which this control should be made.</typeparam>
        public static void ItemStatus<TComp>(
            this EntitySystem.Subscriptions subs,
            Func<Entity<TComp>, Control?> createControl)
            where TComp : IComponent
        {
            subs.SubscribeLocalEvent((Entity<TComp> entity, ref ItemStatusCollectMessage args) =>
            {
                var control = createControl(entity);
                if (control == null)
                    return;

                args.Controls.Add(control);
            });
        }
    }
}
