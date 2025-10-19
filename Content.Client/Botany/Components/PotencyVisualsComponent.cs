// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

namespace Content.Client.Botany.Components;

[RegisterComponent]
public sealed partial class PotencyVisualsComponent : Component
{
    [DataField("minimumScale")]
    public float MinimumScale = 1f;

    [DataField("maximumScale")]
    public float MaximumScale = 2f;
}
