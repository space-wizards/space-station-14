// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.GameStates;

namespace Content.Shared.Xenoarchaeology.Equipment.Components;

/// <summary>
///     Suppress artifact activation, when entity is placed inside this container.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SuppressArtifactContainerSystem))]
public sealed partial class SuppressArtifactContainerComponent : Component;
