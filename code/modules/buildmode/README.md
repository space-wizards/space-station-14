# Buildmode

## Code layout

### Buildmode

Manager for buildmode modes. Contains logic to manage switching between each mode, and presenting a suitable user interface.

### Effects

Special graphics used by buildmode modes for user interface purposes.

### Buildmode Mode

Implementer of buildmode behaviors.

Existing varieties:

+ Basic

	**Description**:

	Allows creation of simple structures consisting of floors, walls, windows, and airlocks.

	**Controls**:

	+ *Left click a turf*:
	
		"Upgrades" the turf based on the following rules below:

		+ Space -> Tiled floor
		+ Simulated floor -> Regular wall
		+ Wall -> Reinforced wall
	
	+ *Right click a turf*:

		"Downgrades" the turf based on the following rules below:

		+ Reinforced wall -> Regular wall
		+ Wall -> Tiled floor
		+ Simulated floor -> Space
	
	+ *Right click an object*:

		Deletes the clicked object.

	+ *Alt+Left click a location*:

		Places an airlock at the clicked location.
	
	+ *Ctrl+Left click a location*:

		Places a window at the clicked location.

+ Advanced

	**Description**:

	Creates an instance of a configurable atom path where you click.

	**Controls**:

	+ *Right click on the mode selector*:

		Choose a path to spawn.
	
	+ *Left click a location* (requires chosen path):

		Place an instance of the chosen path at the location.

	+ *Right click an object*:

		Delete the object.

+ Fill

	**Description**:

	Creates an instance of an atom path on every tile in a chosen region.

	With a special control input, instead deletes everything within the region.

	**Controls**:

	+ *Right click on the mode selector*:

		Choose a path to spawn.

	+ *Left click on a region* (requires chosen path):

		Fill the region with the chosen path.

	+ *Alt+Left click on a region*:

		Deletes everything within the region.

	+ *Right click during region selection*:

		Cancel region selection.

+ Copy

	**Description**:
	
	Take an existing object in the world, and place duplicates with identical attributes where you click.

	May not always work nicely - "deep" variables such as lists or datums may malfunction.

	**Controls**:

	+ *Right click an existing object*:

		Select the clicked object as a template.

	+ *Left click a location* (Requires a selected object as template):

		Place a duplicate of the template at the clicked location.

+ Area Edit

	**Description**:

	Modifies and creates areas.

	The active area will be highlighted in yellow.

	**Controls**:

	+ *Right click the mode selector*:

		Create a new area, and make it active.

	+ *Right click an existing area*:

		Make the clicked area active.

	+ *Left click a turf*:

		When an area is active, adds the turf to the active area.

+ Var Edit

	**Description**:

	Allows for setting and resetting variables of objects with a click.

	If the object does not have the var, will do nothing and print a warning message.

	**Controls**:

	+ *Right click the mode selector*:

		Choose which variable to set, and what to set it to.

	+ *Left click an atom*:

		Change the clicked atom's variables as configured.
	
	+ *Right click an atom*:

		Reset the targeted variable to its original value in the code.

+ Map Generator

	**Description**:

	Fills rectangular regions with algorithmically generated content. Right click during region selection to cancel.

	See the `procedural_mapping` module for the generators themselves.

	**Controls**:

	+ *Right-click on the mode selector*:
	
		Select a map generator from all the generators present in the codebase.
		
	+ *Left click two corners of an area*:

		Use the generator to populate the region.

	+ *Right click during region selection*:

		Cancel region selection.

+ Throwing

	**Description**:

	Select an object with left click, and right click to throw it towards where you clicked.

	**Controls**:

	+ *Left click on a movable atom*:
		
		Select the atom for throwing.
	
	+ *Right click on a location*:

		Throw the selected atom towards that location.

+ Boom

	**Description**:

	Make explosions where you click.

	**Controls**:

	+ *Right click the mode selector*:
	
		Configure the explosion size.

	+ *Left click a location*:
	
		Cause an explosion where you clicked.