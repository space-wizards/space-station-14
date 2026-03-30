using Content.Shared.Movement.Components;
using Content.Shared.Silicons.Borgs;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Shared.Serialization.TypeSerializers.Implementations;

namespace Content.Client.Silicons.Borgs;

/// <summary>
/// Client side logic for borg type switching. Sets up primarily client-side visual information.
/// </summary>
/// <seealso cref="SharedBorgSwitchableTypeSystem"/>
/// <seealso cref="BorgSwitchableTypeComponent"/>
public sealed class BorgSwitchableTypeSystem : SharedBorgSwitchableTypeSystem
{
    [Dependency] private readonly BorgSystem _borgSystem = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly IResourceCache _rescache = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BorgSwitchableTypeComponent, AfterAutoHandleStateEvent>(AfterStateHandler);
        SubscribeLocalEvent<BorgSwitchableTypeComponent, ComponentStartup>(OnComponentStartup);
    }

    private void OnComponentStartup(Entity<BorgSwitchableTypeComponent> ent, ref ComponentStartup args)
    {
        UpdateEntityAppearance(ent);
    }

    private void AfterStateHandler(Entity<BorgSwitchableTypeComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateEntityAppearance(ent);
    }

    protected override void UpdateEntityAppearance(
        Entity<BorgSwitchableTypeComponent> entity,
        BorgTypePrototype prototype)
    {
        if (TryComp(entity, out SpriteComponent? sprite))
        {
            var path = SpriteSpecifierSerializer.TextureRoot / "Mobs/Silicon/Chassis/" + prototype.SpriteBodyState + ".rsi";
            if (_rescache.TryGetResource(path, out RSIResource? rsi))
                _sprite.SetBaseRsi((entity, sprite), rsi.RSI, false);

            _sprite.LayerSetRsiState((entity, sprite), BorgVisualLayers.Body, prototype.SpriteBodyState);
            _sprite.LayerSetRsiState((entity, sprite), BorgVisualLayers.LightStatus, prototype.SpriteToggleLightState);
        }

        if (TryComp(entity, out BorgChassisComponent? chassis))
        {
            _borgSystem.SetMindStates(
                (entity.Owner, chassis),
                prototype.SpriteHasMindState,
                prototype.SpriteNoMindState);

            if (TryComp(entity, out AppearanceComponent? appearance))
            {
                // Queue update so state changes apply.
                _appearance.QueueUpdate(entity, appearance);
            }
        }

        base.UpdateEntityAppearance(entity, prototype);
    }
}
