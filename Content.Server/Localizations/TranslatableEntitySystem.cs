using Content.Server.Localizations.Components;

namespace Content.Server.Localizations;

/// <summary>
///     Handles logic relating to <see cref="TranslatableEntityComponent" />
/// </summary>
public sealed class TranslatableEntitySystem : EntitySystem
{
    [Dependency] private readonly MetaDataSystem _metadata = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TranslatableEntityComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<TranslatableEntityComponent> ent, ref MapInitEvent args)
    {
        var meta = MetaData(ent);

        var newName = Loc.GetString(meta.EntityName);
        var newDesc = Loc.GetString(meta.EntityDescription);

        _metadata.SetEntityName(ent, newName, meta);
        _metadata.SetEntityDescription(ent, newDesc, meta);
    }
}
