using Content.Shared.Clothing.Components;
using Robust.Client.Physics;

namespace Content.Client.Clothing.Systems;

public sealed partial class PilotedByClothingSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PilotedByClothingComponent, UpdateIsPredictedEvent>(OnUpdatePredicted);
    }

    private void OnUpdatePredicted(Entity<PilotedByClothingComponent> entity, ref UpdateIsPredictedEvent args)
    {
        args.BlockPrediction = true;
    }
}
