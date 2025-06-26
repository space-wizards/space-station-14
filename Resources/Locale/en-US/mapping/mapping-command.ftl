cmd-mapping-desc = Create or load a map and teleports you to it.
cmd-mapping-help = Usage: mapping [MapID] [Path] [Grid]
cmd-mapping-server = Only players can use this command.
cmd-mapping-error = An error occurred when creating the new map.
cmd-mapping-try-grid = Failed to load the file as a map. Attempting to load the file as a grid...
cmd-mapping-success-load = Created uninitialized map from file {$path} with id {$mapId}.
cmd-mapping-success-load-grid = Loaded uninitialized grid from file {$path} onto a new map with id {$mapId}.
cmd-mapping-success = Created uninitialized map with id {$mapId}.
cmd-mapping-warning = WARNING: The server is using a debug build. You are risking losing your changes.


# duplicate text from engine load/save map commands.
# I CBF making this PR depend on that one.
cmd-mapping-failure-integer = {$arg} is not a valid integer.
cmd-mapping-failure-float = {$arg} is not a valid float.
cmd-mapping-failure-bool = {$arg} is not a valid bool.
cmd-mapping-nullspace = You cannot load into map 0.
cmd-hint-mapping-id = [MapID]
cmd-mapping-hint-grid = [Grid]
cmd-hint-mapping-path = [Path]
cmd-mapping-exists = Map {$mapId} already exists.
