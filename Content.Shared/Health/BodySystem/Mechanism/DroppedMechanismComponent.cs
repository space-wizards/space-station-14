using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using Content.Shared.BodySystem;
using Robust.Shared.ViewVariables;
using System.Globalization;

namespace Content.Shared.BodySystem {

    /// <summary>
    ///    Component containing the data for a dropped Mechanism entity.
    /// </summary>	
    [RegisterComponent]
    public class DroppedMechanismComponent : Component
    {

        #pragma warning disable CS0649
            [Dependency]
            private IPrototypeManager _prototypeManager;
        #pragma warning restore

        public sealed override string Name => "DroppedMechanism";

        [ViewVariables]
        private Mechanism _containedMechanism;

        public Mechanism ContainedMechanism => _containedMechanism;

        public void InitializeDroppedMechanism(Mechanism data)
        {
            _containedMechanism = data;
            Owner.Name = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(_containedMechanism.Name);
            //TODO: figure out how to do this while in shared since SpriteComponent doesn't exist in it (maybe move to server?)
            /*if (Owner.TryGetComponent<SpriteComponent>(out SpriteComponent component))
            {
                component.LayerSetTexture(0, data.SpritePath);
            }*/
        }
    }
}

