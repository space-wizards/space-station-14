// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Client.GameObjects;
using Robust.Shared.Utility;
using Content.Shared.DeadSpace.Necromorphs.InfectionDead;
using Content.Shared.Rotation;

namespace Content.Client.Necromorphs.InfectionDead;

public sealed class NecromorfSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly SharedRotationVisualsSystem _sharedRotationVisuals = default!;
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

        // Проверяем, существует ли уже слой Necromorf
        if (sprite.LayerMapTryGet(NecromorfLayers.Necromorf, out _))
            return;

        var path = new ResPath(spritePath);

        try
        {
            // Если объект является "животным", применяем особую логику
            if (isAnimal)
            {
                sprite.LayerSetColor(0, new Color(255, 255, 255, 0));

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
}

public enum NecromorfLayers : byte
{
    Necromorf
}
