// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Content.Shared.Sprite;

namespace Content.Client.Sprite;

/// <summary>
/// The non-networked client-only component to track active <see cref="SpriteFadeComponent"/>
/// </summary>
[RegisterComponent, Access(typeof(SpriteFadeSystem))]
public sealed partial class FadingSpriteComponent : Component
{
    [ViewVariables]
    public float OriginalAlpha;
}
