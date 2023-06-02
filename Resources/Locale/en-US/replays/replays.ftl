# Loading screen

replay-loading = Loading ({$cur}/{$total})
replay-loading-reading = Reading Files
replay-loading-processing = Processing Files
replay-loading-spawning = Spawning Entities
replay-loading-initializing = Initializing Entities
replay-loading-starting= Starting Entities
replay-loading-failed = Failed to load replay:
                        {$reason}

# Main Menu
replay-menu-subtext = Replay Client
replay-menu-load = Load Selected Replay
replay-menu-select = Select a Replay
replay-menu-open = Open Replay Folder
replay-menu-none = No replays found.

# Main Menu info box
replay-info-title = Replay Information
replay-info-none-selected = No replay selected
replay-info-invalid = [color=red]Invalid replay selected[/color] 
replay-info-info = {"["}color=gray]Selected:[/color]   {$file}
                   {"["}color=gray]Time:[/color]   {$time}
                   {"["}color=gray]Round ID:[/color]   {$roundId}
                   {"["}color=gray]Duration:[/color]   {$duration}
                   {"["}color=gray]ForkId:[/color]   {$forkId}
                   {"["}color=gray]Version:[/color]   {$version}
                   {"["}color=gray]Engine:[/color]   {$engVersion}
                   {"["}color=gray]Hash:[/color]   {$hash}

# Replay selection window
replay-menu-select-title = Select Replay

# Time controls
replay-dynamic-scrubbing-label = Dynamic Scrubbing

# commands

cmd-replay-play-desc = Resume replay playback.
cmd-replay-play-help = replay_play
cmd-replay-pause-desc = Pause replay playback
cmd-replay-pause-help = replay_pause
cmd-replay-toggle-desc = Resume or pause replay playback.
cmd-replay-toggle-help = replay_toggle
cmd-replay-stop-desc = Stop and unload a replay.
cmd-replay-stop-help = replay_stop
cmd-replay-skip-desc = Skip forwards or backwards in time.
cmd-replay-skip-help = replay_skip <tick or timespan>
cmd-replay-skip-hint = Ticks or timespan (HH:MM:SS).
cmd-replay-set-desc = Jump forwards or backwards to some specific time.
cmd-replay-set-help = replay_set <tick or time>
cmd-replay-set-hint = Tick or timespan (HH:MM:SS), starting from 0.
cmd-replay-error-time = "{$time}" is not an integer or timespan.
cmd-replay-error-args = Wrong number of arguments.