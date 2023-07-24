using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server.MassMedia.Components
{
    [RegisterComponent]
    public sealed class NewsWriteComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        public int Test = 0;
    }
}
