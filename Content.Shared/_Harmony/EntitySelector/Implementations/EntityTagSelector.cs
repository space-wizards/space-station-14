using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Shared._Harmony.EntitySelector.Implementations;

public sealed partial class EntityTagSelector : EntitySelector
{
    private TagSystem _tagSystem = default!;

    [DataField(required: true)]
    public List<ProtoId<TagPrototype>> Tags = new();

    /// <summary>
    /// If false, an entity only needs one of the tags to be matched. If true, an entity needs all of the tags to
    /// be matched.
    /// </summary>
    [DataField]
    public bool RequireAll;

    /// <inheritdoc />
    internal override void Initialize()
    {
        base.Initialize();

        _tagSystem = EntityManager.EntitySysManager.GetEntitySystem<TagSystem>();
    }

    /// <inheritdoc />
    public override bool Matches(EntityUid entity)
    {
        if (!base.Matches(entity))
            return false;

        if (RequireAll)
            return _tagSystem.HasAllTags(entity, Tags);

        return _tagSystem.HasAnyTag(entity, Tags);
    }
}
