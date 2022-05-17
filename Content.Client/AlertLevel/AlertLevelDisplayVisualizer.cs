using System.Linq;
using Content.Shared.AlertLevel;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Utility;

namespace Content.Client.AlertLevel;

public sealed class AlertLevelDisplayVisualizer : AppearanceVisualizer
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

    [DataField("alertVisuals")]
    private readonly Dictionary<string, string> _alertVisuals = new();

    public override void InitializeEntity(EntityUid entity)
    {
        IoCManager.InjectDependencies(this);

        if (!_entityManager.TryGetComponent(entity, out SpriteComponent? sprite))
        {
            return;
        }

        var layer = sprite.AddLayer(new RSI.StateId(_alertVisuals.Values.First()));
        sprite.LayerMapSet(AlertLevelDisplay.Layer, layer);
    }

    public override void OnChangeData(AppearanceComponent component)
    {
        if (!component.TryGetData<string>(AlertLevelDisplay.CurrentLevel, out var level)
            || !_entityManager.TryGetComponent(component.Owner, out SpriteComponent? sprite))
        {
            return;
        }

        if (_alertVisuals.TryGetValue(level, out var visual))
        {
            sprite.LayerSetState(AlertLevelDisplay.Layer, new RSI.StateId(visual));
        }
        else
        {
            sprite.LayerSetState(AlertLevelDisplay.Layer, new RSI.StateId(_alertVisuals.Values.First()));
        }
    }
}
