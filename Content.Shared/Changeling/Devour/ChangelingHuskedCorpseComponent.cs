using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Robust.Shared.GameStates;

namespace Content.Shared.Changeling.Devour;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedChangelingDevourSystem))]
public sealed partial class ChangelingHuskedCorpseComponent : Component
{

}

