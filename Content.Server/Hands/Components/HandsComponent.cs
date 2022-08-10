using Content.Shared.Body.Part;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;

namespace Content.Server.Hands.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedHandsComponent))]
#pragma warning disable 618
    public sealed class HandsComponent : SharedHandsComponent, IBodyPartAdded, IBodyPartRemoved
#pragma warning restore 618
    {
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

        void IBodyPartAdded.BodyPartAdded(BodyPartAddedEventArgs args)
        {
            if (args.Part.PartType != BodyPartType.Hand)
                return;

            // If this annoys you, which it should.
            // Ping Smugleaf.
            var location = args.Part.Symmetry switch
            {
                BodyPartSymmetry.None => HandLocation.Middle,
                BodyPartSymmetry.Left => HandLocation.Left,
                BodyPartSymmetry.Right => HandLocation.Right,
                _ => throw new ArgumentOutOfRangeException()
            };

            _entitySystemManager.GetEntitySystem<SharedHandsSystem>().AddHand(Owner, args.Slot, location);
        }

        void IBodyPartRemoved.BodyPartRemoved(BodyPartRemovedEventArgs args)
        {
            if (args.Part.PartType != BodyPartType.Hand)
                return;

            _entitySystemManager.GetEntitySystem<SharedHandsSystem>().RemoveHand(Owner, args.Slot);
        }
    }
}

