using Robust.Shared.GameObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server.PDA.Events
{
    public class PenSlotChanged : EntityEventArgs
    {
        public IEntity? Item;

        public PenSlotChanged(IEntity? item)
        {
            Item = item;
        }
    }
}
