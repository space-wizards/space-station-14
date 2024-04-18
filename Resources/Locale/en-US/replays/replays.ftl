# Loading Screen

replay-loading = Loading ({$cur}/{$total})
replay-loading-reading = Reading Files
replay-loading-processing = Processing Files
replay-loading-spawning = Spawning Entities
replay-loading-initializing = Initializing Entities
replay-loading-starting= Starting Entities
replay-loading-failed = Failed to load replay. Error:
                        {$reason}
replay-loading-retry = Try load with more exception tolerance - MAY CAUSE BUGS!

# Main Menu
replay-menu-subtext = Replay Client
replay-menu-load = Load Selected Replay
replay-menu-select = Select a Replay
replay-menu-open = Open Replay Folder
replay-menu-none = No replays found.

# Main Menu Info Box
replay-info-title = Replay Information
replay-info-none-selected = No replay selected
replay-info-invalid = [color=red]Invalid replay selected[/color]
replay-info-info = {"["}color=gray]Selected:[/color]  {$name} ({$file})
                   {"["}color=gray]Time:[/color]   {$time}
                   {"["}color=gray]Round ID:[/color]   {$roundId}
                   {"["}color=gray]Duration:[/color]   {$duration}
                   {"["}color=gray]ForkId:[/color]   {$forkId}
                   {"["}color=gray]Version:[/color]   {$version}
                   {"["}color=gray]Engine:[/color]   {$engVersion}
                   {"["}color=gray]Type Hash:[/color]   {$hash}
                   {"["}color=gray]Comp Hash:[/color]   {$compHash}

# Replay selection window
replay-menu-select-title = Select Replay

# Replay related verbs
replay-verb-spectate = Spectate

# command
cmd-replay-spectate-help = replay_spectate [optional entity]
cmd-replay-spectate-desc = Attaches or detaches the local player to a given entity uid.
cmd-replay-spectate-hint = Optional EntityUid
