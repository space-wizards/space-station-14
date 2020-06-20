using System;
using System.Collections.Generic;
using Content.Shared.GameObjects;
using Mono.Cecil;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.BodySystem {

    public abstract class SharedSurgeryToolComponent : Component {
        public override string Name => "SurgeryTool";
        public override uint? NetID => ContentNetIDs.SURGERY;
    }

}

