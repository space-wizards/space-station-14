using Content.Shared.Botany.Components;

namespace Content.Shared.Botany.Systems;

/// <summary>
/// Handles <see cref="MysteriousProduceComponent"/>.
/// </summary>
public sealed class MysteriousProduceSystem : EntitySystem
{
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MysteriousProduceComponent, PlantCopyTraitsEvent>(OnCopyTraits);
        SubscribeLocalEvent<MysteriousProduceComponent, ProduceCreatedEvent>(OnProduceCreated);
    }

    // TODO: mutation

    private void OnCopyTraits(Entity<MysteriousProduceComponent> ent, ref PlantCopyTraitsEvent args)
    {
        EnsureComp<MysteriousProduceComponent>(args.Plant);
    }

    private void OnProduceCreated(Entity<MysteriousProduceComponent> ent, ref ProduceCreatedEvent args)
    {
        var uid = args.Produce;
        var metaData = MetaData(uid);
        _metaData.SetEntityName(uid, metaData.EntityName + "?", metaData);
        var addon = Loc.GetString("botany-mysterious-description-addon");
        var desc = $"{metaData.EntityDescription} {addon}";
        _metaData.SetEntityDescription(uid, desc, metaData);
    }
}
