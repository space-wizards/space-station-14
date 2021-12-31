using System;
using System.Diagnostics.CodeAnalysis;
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
            SubscribeLocalEvent<BarSignComponent, PowerChangedEvent>(UpdateBarSignVisuals);
        }

        private void UpdateBarSignVisuals(EntityUid owner, BarSignComponent component, PowerChangedEvent args)
        {
            if (component.LifeStage is < ComponentLifeStage.Initialized or > ComponentLifeStage.Running) return;

            if (!TryComp(owner, out SpriteComponent? sprite))
            {
                Logger.ErrorS("barSign", "Barsign is missing sprite component");
                return;
            }

            if (!TryGetBarSignPrototype(component, out var prototype))
            {
                prototype = Setup(owner, component);
            }

            if (args.Powered)
            {
                sprite.LayerSetState(0, prototype.Icon);
                sprite.LayerSetShader(0, "unshaded");
            }
            else
            {
                sprite.LayerSetState(0, "empty");
                sprite.LayerSetShader(0, "shaded");
            }
        }

        private bool TryGetBarSignPrototype(BarSignComponent component, [NotNullWhen(true)] out BarSignPrototype? prototype)
        {
            if (component.CurrentSign != null)
            {
                if (_prototypeManager.TryIndex(component.CurrentSign, out prototype))
                {
                    return true;
                }
                Logger.ErrorS("barSign", $"Invalid bar sign prototype: \"{component.CurrentSign}\"");
            }
            else
            {
                prototype = null;
            }
            return false;
        }

        private BarSignPrototype Setup(EntityUid owner, BarSignComponent component)
        {
            var prototypes = _prototypeManager
                .EnumeratePrototypes<BarSignPrototype>()
                .Where(p => !p.Hidden)
                .ToList();

            var newPrototype = _random.Pick(prototypes);

            var meta = Comp<MetaDataComponent>(owner);
            meta.EntityName = newPrototype.Name != string.Empty ? newPrototype.Name : Loc.GetString("barsign-component-name");
            meta.EntityDescription = newPrototype.Description;

            component.CurrentSign = newPrototype.ID;
            return newPrototype;
        }
    }
}
