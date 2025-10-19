// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

namespace Content.Server.Vocalization.Components;

/// <summary>
/// Used in combination with <see cref="VocalizerComponent"/>.
/// Blocks any attempts to vocalize if the entity has an <see cref="ApcPowerReceiverComponent"/>
/// and is currently unpowered.
/// </summary>
[RegisterComponent]
public sealed partial class VocalizerRequiresPowerComponent : Component;
