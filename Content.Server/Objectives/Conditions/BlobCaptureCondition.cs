using Content.Server.Blob;
using Content.Server.Objectives.Interfaces;
using Content.Shared.Blob;
using JetBrains.Annotations;
using Robust.Shared.Utility;

namespace Content.Server.Objectives.Conditions;

[UsedImplicitly]
[DataDefinition]
public sealed class BlobCaptureCondition : IObjectiveCondition
{
    private Mind.Mind? _mind;
    private int _target;

    public IObjectiveCondition GetAssigned(Mind.Mind mind)
    {
        return new BlobCaptureCondition
        {
            _mind = mind,
            _target = 400
        };
    }

    public string Title => Loc.GetString("objective-condition-blob-capture-title");

    public string Description => Loc.GetString("objective-condition-blob-capture-description", ("count", _target));

    public SpriteSpecifier Icon => new SpriteSpecifier.Rsi(new ResPath("Mobs/Aliens/blob.rsi"), "blob_nuke_overlay");

    public float Progress
    {
        get
        {
            var entMan = IoCManager.Resolve<IEntityManager>();
            // prevent divide-by-zero
            if (_target == 0)
                return 1f;

            if (_mind?.OwnedEntity == null)
                return 0f;

            if (!entMan.TryGetComponent<BlobObserverComponent>(_mind.OwnedEntity, out var blobObserverComponent)
                || !entMan.TryGetComponent<BlobCoreComponent>(blobObserverComponent.Core, out var blobCoreComponent))
            {
                return 0f;
            }

            return (float) blobCoreComponent.BlobTiles.Count / (float) _target;
        }
    }

    public float Difficulty => 4.0f;

    public bool Equals(IObjectiveCondition? other)
    {
        return other is BlobCaptureCondition cond && Equals(_mind, cond._mind) && _target == cond._target;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj is BlobCaptureCondition cond && cond.Equals(this);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_mind?.GetHashCode() ?? 0, _target);
    }
}
