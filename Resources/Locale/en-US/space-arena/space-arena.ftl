space-arena-mode-unknown = Unknown mode
space-arena-mode-team-deathmatch = Team Deathmatch
space-arena-mode-drawing = Drawing
space-arena-lobby-unknown-host = Unknown player
space-arena-lobby-player-joined = {$player} joined the lobby. Players: {$players}/{$max}.

space-arena-match-countdown = The fight will begin in {$seconds} seconds!
space-arena-match-fight-start = Fight!
space-arena-match-victory = You won!
space-arena-match-defeat = You lost!
space-arena-match-draw = The fight ended without a winner.

space-arena-lobby-window-title = SpaceArena
space-arena-lobby-heading = Player lobbies
space-arena-lobby-description = Create a room or join another player's match. The host starts the game when everyone is ready.
space-arena-lobby-create-heading = Create lobby
space-arena-lobby-mode-label = Mode
space-arena-lobby-arena-label = Arena
space-arena-lobby-create-button = Create
space-arena-lobby-arena-option = {$arena} · {$format}
space-arena-lobby-weapon-preview-tooltip = Primary arena weapon
space-arena-lobby-membership-none = You are not in a lobby.
space-arena-lobby-membership-joined = You have joined a lobby.
space-arena-lobby-membership-active = Match in progress. Lobby changes are unavailable.
space-arena-lobby-membership-spectating = You are observing a match.
space-arena-lobby-leave-button = Leave lobby
space-arena-lobby-list-heading = Lobbies and active matches
space-arena-lobby-list-empty = No lobbies or active matches. Create the first one!
space-arena-lobby-room-title = {$host} · {$mode}
space-arena-lobby-room-details = {$arena} · {$players}/{$max} players · minimum {$min} · {$state}
space-arena-lobby-start-button = Start match
space-arena-lobby-start-disabled = At least {$min} players are required.
space-arena-lobby-you-joined = Joined
space-arena-lobby-join-button = Join
space-arena-lobby-spectate-button = Observe
space-arena-lobby-you-spectate = Observing
space-arena-lobby-state-waiting = Waiting
space-arena-lobby-state-preparing = Preparing
space-arena-lobby-state-countdown = Countdown
space-arena-lobby-state-active = In progress
space-arena-lobby-state-finishing = Finishing
space-arena-hud-button = ARENAS
space-arena-hud-button-tooltip = Open SpaceArena lobbies
space-arena-hud-return-to-hub-button = RETURN TO HUB
space-arena-hud-return-to-hub-tooltip = Leave the current activity and return to the SpaceArena hub

ent-ComputerSpaceArenaLobby = SpaceArena lobby terminal
    .desc = Browse player-created rooms, join a match, or host your own.
ent-SpaceArenaLobbyComputerCircuitboard = SpaceArena lobby terminal board
    .desc = A computer printed circuit board for a SpaceArena lobby terminal.

cmd-arena-create-desc = Creates a waiting SpaceArena match.
cmd-arena-create-help = Usage: arena_create <mode prototype> <arena map prototype>
cmd-arena-create-mode-hint = Match mode prototype
cmd-arena-create-map-hint = Arena map prototype
cmd-arena-create-failed = Could not create mode {$mode} on arena {$arena}.
cmd-arena-create-success = Created match {$match}.

cmd-arena-join-desc = Adds the executing admin to a waiting SpaceArena match.
cmd-arena-join-help = Usage: arena_join <match entity>
cmd-arena-player-required = This command requires an attached player.
cmd-arena-join-failed = Could not join that match.
cmd-arena-join-success = Joined the waiting match.

cmd-arena-start-desc = Starts a waiting SpaceArena match.
cmd-arena-start-help = Usage: arena_start <match entity>
cmd-arena-start-failed = Could not start that match. Check its state and minimum player count.
cmd-arena-start-success = Match startup began.

cmd-arena-finish-desc = Finishes an active SpaceArena match.
cmd-arena-finish-help = Usage: arena_finish <match entity>
cmd-arena-finish-failed = Could not finish that match.
cmd-arena-finish-success = Match is finishing.

cmd-arena-leave-desc = Leaves the current SpaceArena match.
cmd-arena-leave-help = Usage: arena_leave
cmd-arena-leave-failed = You are not in a SpaceArena match.
cmd-arena-leave-success = You left the match.

cmd-arena-list-desc = Lists current SpaceArena matches.
cmd-arena-list-help = Usage: arena_list
cmd-arena-list-empty = There are no SpaceArena matches.
cmd-arena-list-entry = [{$match}] {$mode}: {$state}, {$players}/{$capacity} players
space-arena-preset-title = SpaceArena
space-arena-preset-description = Social hub with player-hosted arena matches and minigames.
