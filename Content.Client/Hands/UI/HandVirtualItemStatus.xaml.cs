// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Robust.Client.UserInterface;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.Hands.UI
{
    public sealed class HandVirtualItemStatus : Control
    {
        public HandVirtualItemStatus()
        {
            RobustXamlLoader.Load(this);
        }
    }
}
