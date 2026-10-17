using Content.Server.Stack;
using Content.Shared.Gibbing;
using Content.Shared.Storage.Components;
using Content.Shared.Whitelist;
using Content.Shared.Xenoarchaeology.Equipment;
using Content.Shared.Xenoarchaeology.Equipment.Components;
using Robust.Shared.Collections;

namespace Content.Server.Xenoarchaeology.Equipment.Systems;

/// <inheritdoc/>
public sealed partial class ArtifactCrusherSystem : SharedArtifactCrusherSystem
{
    [Dependency] private GibbingSystem _gibbing = default!;
    [Dependency] private StackSystem _stack = default!;
    [Dependency] private EntityWhitelistSystem _whitelistSystem = default!;

    // TODO: Move to shared once StackSystem spawning is in Shared
    public override void FinishCrushing(Entity<ArtifactCrusherComponent, EntityStorageComponent> ent)
    {
        var (_, crusher, storage) = ent;
        StopCrushing((ent, ent.Comp1), false);
        AudioSystem.PlayPvs(crusher.CrushingCompleteSound, ent);
        crusher.CrushingSoundEntity = null;
        Dirty(ent, ent.Comp1);

        var contents = new ValueList<EntityUid>(storage.Contents.ContainedEntities);
        var coords = Transform(ent).Coordinates;
        foreach (var contained in contents)
        {
            var lockedNodes = 0;
            if (ArtifactQuery.HasComp(contained) && _whitelistSystem.IsWhitelistPass(crusher.CrushingWhitelist, contained))
                lockedNodes = MakeShards(contained, coords, crusher);

            if (lockedNodes > 0)
            {
                if (lockedNodes > ent.Comp1.MaxFragments) //limit fragment spawning
                    lockedNodes = ent.Comp1.MaxFragments;
                var stacks = _stack.SpawnMultipleAtPosition(crusher.FragmentStackProtoId, lockedNodes, coords);
                foreach (var stack in stacks)
                {
                    ContainerSystem.Insert((stack, null, null, null), crusher.OutputContainer);
                }
            }

            var gibs = _gibbing.Gib(contained);
            foreach (var gib in gibs)
            {
                ContainerSystem.Insert((gib, null, null, null), crusher.OutputContainer);
            }
        }
    }
}
