using Content.Shared.Item;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.Items;

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
    public List<ItemStatusEntry> Controls = [];

    public void Add(Control control, ProtoId<ItemStatusPrototype> prototype)
    {
        Controls.Add(new ItemStatusEntry(control, prototype));
    }
}

public readonly record struct ItemStatusEntry(Control Control, ProtoId<ItemStatusPrototype> Prototype);

/// <summary>
/// Extension methods for registering item status controls.
/// </summary>
/// <seealso cref="ItemStatusCollectMessage"/>
public static class ItemStatusRegisterExt
{
    public static readonly ProtoId<ItemStatusPrototype> ItemStatusDefault = "Default";

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
    /// <param name="prototype">The item status prototype.</param>
    /// <typeparam name="TComp">The type of component for which this control should be made.</typeparam>
    public static void ItemStatus<TComp>(
        this EntitySystem.Subscriptions subs,
        Func<Entity<TComp>, Control?> createControl,
        ProtoId<ItemStatusPrototype>? prototype = null)
        where TComp : IComponent
    {
        var statusPrototype = prototype ?? ItemStatusDefault;

        subs.SubscribeLocalEvent((Entity<TComp> entity, ref ItemStatusCollectMessage args) =>
        {
            var control = createControl(entity);
            if (control == null)
                return;

            args.Add(control, statusPrototype);
        });
    }
}
