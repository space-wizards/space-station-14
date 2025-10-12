# FTLdiskburner
cmd-ftldisk-desc = Creates an FTL coordinates disk to sail to the map the given EntityID is/on
cmd-ftldisk-help = ftldisk [EntityID]
cmd-ftldisk-no-transform = Entity { $destination } has no Transform Component!
cmd-ftldisk-no-map = Entity { $destination } has no map!
cmd-ftldisk-no-map-comp = Entity { $destination } is somehow on map { $map } with no map component.
cmd-ftldisk-map-not-init = Entity { $destination } is on map { $map } which is not initialized! Check it's safe to initialize, then initialize the map first or the players will be stuck in place!
cmd-ftldisk-map-paused = Entity { $desintation } is on map { $map } which is paused! Please unpause the map first or the players will be stuck in place.
cmd-ftldisk-planet = Entity { $desintation } is on planet map { $map } and will require an FTL point. It may already exist.
cmd-ftldisk-already-dest-not-enabled = Entity { $destination } is on map { $map } that already has an FTLDestinationComponent, but it is not Enabled! Set this manually for safety.
cmd-ftldisk-requires-ftl-point = Entity { $destination } is on map { $map } that requires a FTL point to travel to! It may already exist.
cmd-ftldisk-hint = Map netID
