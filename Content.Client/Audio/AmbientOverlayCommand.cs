// SPDX-FileCopyrightText: Space Station 14 Contributors <https://spacestation14.com/about/about/>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.Console;

namespace Content.Client.Audio;

public sealed class AmbientOverlayCommand : LocalizedEntityCommands
{
    [Dependency] private readonly AmbientSoundSystem _ambient = default!;

    public override string Command => "showambient";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        _ambient.OverlayEnabled ^= true;

        shell.WriteLine(Loc.GetString($"cmd-showambient-status", ("status", _ambient.OverlayEnabled)));
    }
}
