cmd-addatmos-desc = Adds atmos support to a grid.
cmd-addatmos-help = Usage: addatmos <GridId>
cmd-addatmos-grid-already-has-atmos = Grid already has an atmosphere.
cmd-addatmos-success = Added atmosphere to grid {$grid}.

cmd-addgas-desc = Adds gas at a certain position.
cmd-addgas-help = Usage: addgas <X> <Y> <GridEid> <Gas> <moles>
cmd-addgas-invalid-coordinates = Invalid coordinates or tile.

cmd-deletegas-desc = Removes all gases from a grid, or just of one type if specified.
cmd-deletegas-help = Usage: deletegas <GridId> <Gas> / deletegas <GridId> / deletegas <Gas> / deletegas
cmd-deletegas-no-entity = You have no entity to get a grid from.
cmd-deletegas-must-specify-grid = A grid must be specified when the command isn't used by a player.
cmd-deletegas-not-on-grid = You aren't on a grid to delete gas from.
cmd-deletegas-invalid-gas-specified = {$gas} is not a valid gas name.
cmd-deletegas-invalid-grid-id = No grid exists with id {$id}!
cmd-deletegas-success-no-gas = Removed {$moles} moles from {$tiles} tiles.
cmd-deletegas-success-gas = Removed {$moles} moles of gas {$gas} from {$tiles} tiles.

cmd-fillgas-desc = Adds gas to all tiles in a grid.
cmd-fillgas-help = Usage: fillgas <GridEid> <Gas> <moles>

cmd-listgases-desc = Prints a list of gases and their indices.
cmd-listgases-help = Usage: listgases

cmd-removegas-desc = Removes an amount of gases.
cmd-removegas-help = Usage: removegas <X> <Y> <GridId> <amount> <ratio>
                     If <ratio> is true, amount will be treated as the ratio of gas to be removed.

cmd-setatmostemp-desc = Sets a grid's temperature (in kelvin).
cmd-setatmostemp-help = Usage: setatmostemp <GridId> <Temperature>
cmd-setatmostemp-invalid-temperature = Invalid temperature.
cmd-setatmostemp-success = Changed the temperature of {$tiles} tiles.

cmd-settemp-desc = Sets a tile's temperature (in kelvin).
cmd-settemp-help = Usage: settemp <X> <Y> <GridId> <Temperature>

cmd-showatmos-desc = Toggles seeing atmos debug overlay.
cmd-showatmos-help = Usage: showatmos
cmd-showatmos-status = Atmos overlay display set to {$status}.
