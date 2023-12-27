using Content.Client.RoundEnd;
using Content.Shared.GameTicking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Client.GameTicking.Managers
{
    public class ClientGameTicker_RoundEndData_Container
    {
        //this will just keep data from the current round end - and clear it when the next round starts. Allows us to reopen the window, without any bugs (like ending the round after it's already ended, etc.)
        public RoundEndMessageEvent? _message { get; set; }
    }
}
