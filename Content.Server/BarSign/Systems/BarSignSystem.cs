using System.Linq;
using Content.Server.Power.Components;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.BarSign.Systems
{
    public class BarSignSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<BarSignComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<BarSignComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<BarSignComponent, PowerChangedEvent>(OnPowerChanged);
        }

        public void SetBarSign(BarSignComponent component, string barSign)
        {
            component.CurrentSign = barSign;
            UpdateBarSignVisuals(component);
        }

        private void OnComponentInit(EntityUid uid, BarSignComponent component, ComponentInit args)
        {
            UpdateBarSignVisuals(component);
        }

        private void OnMapInit(EntityUid uid, BarSignComponent component, MapInitEvent args)
        {
            if (component.CurrentSign != null)
            {
                return;
            }

            var prototypes = _prototypeManager.EnumeratePrototypes<BarSignPrototype>().Where(p => !p.Hidden)
                .ToList();
            var prototype = _random.Pick(prototypes);

            component.CurrentSign = prototype.ID;
        }

        private void OnPowerChanged(EntityUid uid, BarSignComponent component, PowerChangedEvent args)
        {
            UpdateBarSignVisuals(component);
        }

        private void UpdateBarSignVisuals(BarSignComponent component)
        {
            if (component.CurrentSign == null)
            {
                return;
            }

            if (!_prototypeManager.TryIndex(component.CurrentSign, out BarSignPrototype? prototype))
            {
                Logger.ErrorS("barSign", $"Invalid bar sign prototype: \"{component.CurrentSign}\"");
                return;
            }

            if (component.Owner.TryGetComponent(out SpriteComponent? sprite))
            {
                if (!component.Owner.TryGetComponent(out ApcPowerReceiverComponent? receiver) || !receiver.Powered)
                {
                    sprite.LayerSetState(0, "empty");
                    sprite.LayerSetShader(0, "shaded");
                }
                else
                {
                    sprite.LayerSetState(0, prototype.Icon);
                    sprite.LayerSetShader(0, "unshaded");
                }
            }

            if (!string.IsNullOrEmpty(prototype.Name))
            {
                component.Owner.Name = prototype.Name;
            }
            else
            {
                component.Owner.Name = Loc.GetString("barsign-component-name");
            }

            component.Owner.Description = prototype.Description;
        }
    }
}
