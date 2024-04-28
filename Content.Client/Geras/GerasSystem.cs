using Content.Shared.Humanoid;
using Content.Shared.Geras;
using Robust.Client.GameObjects;

namespace Content.Client.Geras;

public sealed class GerasSystem : SharedGerasSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<GerasChildEntity>(OnGerasChildEntity);
    }

    private void OnGerasChildEntity(GerasChildEntity ev)
    {

        var parentUid = GetEntity(ev.ParentUid);
        var childUid = GetEntity(ev.ChildUid);

        var skinColor = Color.Green;

        if (!TryComp<HumanoidAppearanceComponent>(parentUid, out var appearanceComp))
        {
            return;
        }
        if (TryComp<SpriteComponent>(childUid, out var sprite))
        {
            skinColor = appearanceComp.SkinColor;
            sprite.Color = skinColor;
        }
        return;
    }
}
