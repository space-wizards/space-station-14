// SPDX-FileCopyrightText: 2025 Starlight
// SPDX-License-Identifier: Starlight-MIT

using Content.Shared.GameTicking;
using Robust.Shared.GameStates;

namespace Content.Shared._Starlight.Character.Info.Components;


/// <summary>
/// Stores exploitable roleplaying information that is only visible to the possessing player (or paradox clones)
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedGameTicker), typeof(SLSharedCharacterInfoSystem))]
public sealed partial class CharacterSecretsComponent : Component
{
    public override bool SendOnlyToOwner => true;

    [DataField, AutoNetworkedField] public string Secrets = string.Empty;
}