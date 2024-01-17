using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server.Store.Events;

public record struct StorePurchasedActionEvent(EntityUid Purchaser, EntityUid Action);
