using Content.Server.Audio;
using Content.Shared.Audio;
using Content.Shared.Interaction;
using Content.Shared.Smoking;
using Content.Shared.Temperature;
using Robust.Server.GameObjects;

namespace Content.Server._Impstation.FireStructure;

public sealed class FireStructureSystem : EntitySystem
{
    [Dependency] private readonly AmbientSoundSystem _ambient = default!;
    [Dependency] private readonly PointLightSystem _light = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FireStructureComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(Entity<FireStructureComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || ent.Comp.CurrentState != SmokableState.Unlit)
            return;

        var isHotEvent = new IsHotEvent();
        RaiseLocalEvent(args.Used, isHotEvent);

        if (!isHotEvent.IsHot)
            return;

        LightFire(ent);
        args.Handled = true;
    }

    private void LightFire(Entity<FireStructureComponent> ent)
    {
        if (ent.Comp.AmbientSound != null)
        {
            var component = EnsureComp<AmbientSoundComponent>(ent);
            var ambient = ent.Comp.AmbientSound;
            _ambient.SetRange(ent, ambient.Range, component);
            _ambient.SetVolume(ent, ambient.Volume, component);
            _ambient.SetSound(ent, ambient.Sound, component);
        }

        if (ent.Comp.PointLight != null)
        {
            var component = EnsureComp<PointLightComponent>(ent);
            var light = ent.Comp.PointLight;
            _light.SetColor(ent, light.Color, component);
            _light.SetEnergy(ent, light.Energy, component);
            _light.SetSoftness(ent, light.Softness, component);
            _light.SetCastShadows(ent, light.CastShadows, component);
            _light.SetRadius(ent, light.Radius, component);
        }
    }
}
