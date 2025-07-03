using Content.Shared.DragDrop;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Kitchen;
using Content.Shared.Kitchen.Components;

namespace Content.Server.Kitchen.EntitySystems;

/// <inheritdoc />
public sealed class KitchenSpikeSystem : SharedKitchenSpikeSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KitchenSpikeComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<KitchenSpikeComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<KitchenSpikeComponent, CanDropTargetEvent>(OnCanDrop);
        SubscribeLocalEvent<KitchenSpikeComponent, SuicideByEnvironmentEvent>(OnSuicideByEnvironment);
        SubscribeLocalEvent<KitchenSpikeComponent, SpikeDoAfterEvent>(OnDoAfter);
    }

    private void OnInteractHand(Entity<KitchenSpikeComponent> entity, ref InteractHandEvent args)
    {

    }

    private void OnInteractUsing(Entity<KitchenSpikeComponent> entity, ref InteractUsingEvent args)
    {

    }

    private void OnCanDrop(Entity<KitchenSpikeComponent> entity, ref CanDropTargetEvent args)
    {

    }

    private void OnSuicideByEnvironment(Entity<KitchenSpikeComponent> entity, ref SuicideByEnvironmentEvent args)
    {

    }

    private void OnDoAfter(Entity<KitchenSpikeComponent> entity, ref SpikeDoAfterEvent args)
    {

    }
}
