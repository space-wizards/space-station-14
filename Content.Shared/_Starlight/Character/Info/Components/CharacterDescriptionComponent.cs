// SPDX-FileCopyrightText: 2025 Starlight
// SPDX-License-Identifier: Starlight-MIT

using Content.Shared.GameTicking;
using Robust.Shared.GameStates;

namespace Content.Shared._Starlight.Character.Info.Components;

/// <summary>
/// Stores a detailed description of the character (FlavorText)
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedGameTicker), typeof(SLSharedCharacterInfoSystem))]
public sealed partial class CharacterDescriptionComponent : Component
{
    [DataField, AutoNetworkedField] public string Description = string.Empty;
}