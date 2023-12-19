using Content.Shared.Storage.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;

namespace Content.Shared.Xenoarchaeology.Equipment;

/// <summary>
/// This handles logic relating to <see cref="ArtifactCrusherComponent"/>
/// </summary>
public abstract class SharedArtifactCrusherSystem : EntitySystem
{
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;
    [Dependency] protected readonly SharedAudioSystem AudioSystem = default!;
    [Dependency] protected readonly SharedContainerSystem ContainerSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ArtifactCrusherComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ArtifactCrusherComponent, StorageAfterOpenEvent>(OnStorageAfterOpen);
    }

    private void OnInit(Entity<ArtifactCrusherComponent> ent, ref ComponentInit args)
    {
        ent.Comp.OutputContainer = ContainerSystem.EnsureContainer<Container>(ent, ent.Comp.OutputContainerName);
    }

    private void OnStorageAfterOpen(Entity<ArtifactCrusherComponent> ent, ref StorageAfterOpenEvent args)
    {
        StopCrushing(ent);
        ContainerSystem.EmptyContainer(ent.Comp.OutputContainer);
    }

    public void StopCrushing(Entity<ArtifactCrusherComponent> ent, bool early = true)
    {
        var (_, crusher) = ent;

        if (!crusher.Crushing)
            return;

        crusher.Crushing = false;
        Appearance.SetData(ent, ArtifactCrusherVisuals.Crushing, false);

        if (early)
        {
            AudioSystem.Stop(crusher.CrushingSoundEntity?.Item1, crusher.CrushingSoundEntity?.Item2);
            crusher.CrushingSoundEntity = null;
        }

        Dirty(ent, ent.Comp);
    }
}
