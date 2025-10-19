// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Content.Client.UserInterface.Controls;

public sealed class HSpacer : Control
{
    public float Spacing { get => MinHeight; set => MinHeight = value; }
    public HSpacer()
    {
        MinHeight = Spacing;
    }
    public HSpacer(float height = 5)
    {
        Spacing = height;
        MinHeight = height;
    }
}
