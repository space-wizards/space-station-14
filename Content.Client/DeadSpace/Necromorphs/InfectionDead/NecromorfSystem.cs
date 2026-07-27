// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Client.GameObjects;
using Robust.Shared.Utility;
using Content.Shared.DeadSpace.Necromorphs.InfectionDead;
using Content.Shared.Rotation;
using System.Linq;

namespace Content.Client.Necromorphs.InfectionDead;

public sealed class NecromorfSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<RequestNecroficationEvent>(HandleNecroficationEvent);
    }

    private void HandleNecroficationEvent(RequestNecroficationEvent msg)
    {
        var uid = _entityManager.GetEntity(msg.NetEntity);

        UpdateLayer(uid, msg.Sprite, msg.State, msg.IsAnimal);
    }

    public void UpdateLayer(EntityUid uid, string spritePath, string state, bool isAnimal)
    {
        // Проверяем путь к спрайту
        if (string.IsNullOrEmpty(spritePath))
            return;

        if (!_entityManager.EntityExists(uid))
            return;

        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        // Для животных сохраняем специальную логику: их базовые слои могут восстановиться
        // после обновления внешности, поэтому скрываем их при каждой синхронизации.
        if (isAnimal)
            HideOriginalLayers((uid, sprite));

        // Проверяем, существует ли уже слой Necromorf
        if (_sprite.LayerMapTryGet((uid, sprite), NecromorfLayers.Necromorf, out var necromorfLayer, false))
        {
            _sprite.LayerSetVisible((uid, sprite), necromorfLayer, true);
            return;
        }

        var path = new ResPath(spritePath);

        try
        {
            // Сохраняем особый поворот некроморфов-животных.
            if (isAnimal)
            {
                if (TryComp<RotationVisualsComponent>(uid, out var rotationVisualsComp))
                {
                    rotationVisualsComp.DefaultRotation = Angle.FromDegrees(90);
                }
                else
                {
                    var newRotationVisualsComp = new RotationVisualsComponent
                    {
                        DefaultRotation = Angle.FromDegrees(90)
                    };
                    AddComp(uid, newRotationVisualsComp);
                }
            }

            var index = sprite.AddLayer(state, path);

            sprite.LayerMapSet(NecromorfLayers.Necromorf, index);
            sprite.LayerSetShader(index, "shaded");
        }
        catch (Exception ex)
        {
            Log.Error($"[NecromorfSystem] Failed to update sprite layer for entity {uid}. Exception: {ex.Message}\n{ex.StackTrace}");
        }
    }

    private void HideOriginalLayers(Entity<SpriteComponent> entity)
    {
        var spriteEntity = entity.AsNullable();
        var originalRsi = entity.Comp.BaseRSI;
        var necromorfLayer = _sprite.LayerMapTryGet(spriteEntity, NecromorfLayers.Necromorf, out var index, false)
            ? index
            : -1;

        if (originalRsi == null)
            return;

        for (var i = 0; i < entity.Comp.AllLayers.Count(); i++)
        {
            if (i != necromorfLayer &&
                ReferenceEquals(_sprite.LayerGetEffectiveRsi(spriteEntity, i), originalRsi))
            {
                _sprite.LayerSetVisible(spriteEntity, i, false);
            }
        }
    }
}

public enum NecromorfLayers : byte
{
    Necromorf
}
