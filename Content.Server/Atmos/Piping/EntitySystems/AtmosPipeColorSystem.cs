using Content.Server.Atmos.Piping.Components;
using Content.Shared.Atmos.Piping;

namespace Content.Server.Atmos.Piping.EntitySystems
{
    public sealed class AtmosPipeColorSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<AtmosPipeColorComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<AtmosPipeColorComponent, ComponentShutdown>(OnShutdown);
        }

        private void OnStartup(EntityUid uid, AtmosPipeColorComponent component, ComponentStartup args)
        {
            if (!EntityManager.TryGetComponent(uid, out AppearanceComponent? appearance))
                return;

            appearance.SetData(PipeColorVisuals.Color, component.Color);
        }

        private void OnShutdown(EntityUid uid, AtmosPipeColorComponent component, ComponentShutdown args)
        {
            if (!EntityManager.TryGetComponent(uid, out AppearanceComponent? appearance))
                return;

            appearance.SetData(PipeColorVisuals.Color, Color.White);
        }

        public void SetColor(EntityUid uid, AtmosPipeColorComponent component, Color color)
        {
            component.Color = color;

            if (!EntityManager.TryGetComponent(uid, out AppearanceComponent? appearance))
                return;

            appearance.SetData(PipeColorVisuals.Color, color);
        }
    }
}
