using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Robust.Shared.Prototypes;

namespace Content.Shared._Starlight.Medical.Limbs;
public interface IImplantable
{
}
public partial interface IWithAction : IImplantable
{
    public bool EntityIcon { get; } // It shouldn't be here, but I’m too lazy to redo everything.

    public EntProtoId Action { get; }

    public EntityUid? ActionEntity { get; set; }
}
