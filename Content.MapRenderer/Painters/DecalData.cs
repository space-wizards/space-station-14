// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Content.Shared.Decals;

namespace Content.MapRenderer.Painters;

public readonly record struct DecalData(Decal Decal, float X, float Y);
