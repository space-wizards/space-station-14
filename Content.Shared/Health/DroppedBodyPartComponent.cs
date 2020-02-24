using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using Content.Shared.BodySystem;
using Robust.Shared.ViewVariables;
using Robust.Client.GameObjects;
using Robust.Client.Interfaces.GameObjects.Components;

namespace Content.Shared.BodySystem {

    [RegisterComponent]
    public class DroppedBodyPartComponent : Component {

        #pragma warning disable CS0649
            [Dependency]
            private IPrototypeManager _prototypeManager;
        #pragma warning restore

        public sealed override string Name => "DroppedBodyPart";

        [ViewVariables]
        private BodyPart _containedBodyPart;

        public void TransferBodyPartData(BodyPart data)
        {
            _containedBodyPart = data;
            Owner.Name = _containedBodyPart.Name;
            if(Owner.TryGetComponent<ISpriteComponent>(out ISpriteComponent test))
                test.LayerSetState(0, _containedBodyPart.SpritePath);
        }
    }
}
