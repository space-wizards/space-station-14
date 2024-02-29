game-ticker-restart-round = Restarting round...
game-ticker-start-round = The round is starting now...
game-ticker-start-round-cannot-start-game-mode-fallback = Failed to start {$failedGameMode} mode! Defaulting to {$fallbackMode}...
game-ticker-start-round-cannot-start-game-mode-restart = Failed to start {$failedGameMode} mode! Restarting round...
game-ticker-start-round-invalid-map = Selected map {$map} is inelligible for gamemode {$mode}. Gamemode may not function as intended...
game-ticker-unknown-role = Unknown
game-ticker-delay-start = Round start has been delayed for {$seconds} seconds.
game-ticker-pause-start = Round start has been paused.
game-ticker-pause-start-resumed = Round start countdown is now resumed.
game-ticker-player-join-game-message = Welcome to Space Station 14! If this is your first time playing, be sure to read the game rules, and don't be afraid to ask for help in LOOC (local OOC) or OOC (usually available only between rounds).
game-ticker-get-info-text = Hi and welcome to [color=white]Space Station 14![/color]
                            The current round is: [color=white]#{$roundId}[/color]
                            The current player count is: [color=white]{$playerCount}[/color]
                            The current map is: [color=white]{$mapName}[/color]
                            The current game mode is: [color=white]{$gmTitle}[/color]
                            >[color=yellow]{$desc}[/color]
game-ticker-get-info-preround-text = Hi and welcome to [color=white]Space Station 14![/color]
                            The current round is: [color=white]#{$roundId}[/color]
                            The current player count is: [color=white]{$playerCount}[/color] ([color=white]{$readyCount}[/color] {$readyCount ->
                                [one] is
                                *[other] are
                            } ready)
                            The current map is: [color=white]{$mapName}[/color]
                            The current game mode is: [color=white]{$gmTitle}[/color]
                            >[color=yellow]{$desc}[/color]
game-ticker-no-map-selected = [color=yellow]Map not yet selected![/color]
game-ticker-player-no-jobs-available-when-joining = When attempting to join to the game, no jobs were available.

# Displayed in chat to admins when a player joins
player-join-message = Player {$name} joined.
player-first-join-message = Player {$name} joined for the first time.

# Displayed in chat to admins when a player leaves
player-leave-message = Player {$name} left.

latejoin-arrival-announcement = {$character} ({$job}) has arrived at the station!
latejoin-arrival-sender = Station
latejoin-arrivals-direction = A shuttle transferring you to your station will arrive shortly.
latejoin-arrivals-direction-time = A shuttle transferring you to your station will arrive in {$time}.

preset-not-enough-ready-players = Can't start {$presetName}. Requires {$minimumPlayers} players but we have {$readyPlayersCount}.
preset-no-one-ready = Can't start {$presetName}. No players are ready.