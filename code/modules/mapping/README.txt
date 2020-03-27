The code in this module originally evolved from dmm_suite and has since been
specialized for SS13 and otherwise tweaked to fit /tg/station's needs.

dmm_suite version 1.0
	Released January 30th, 2011.

NOTE: Map saving functionality removed

defines the object /dmm_suite
	- Provides the proc load_map()
		- Loads the specified map file onto the specified z-level.
	- provides the proc write_map()
		- Returns a text string of the map in dmm format
			ready for output to a file.
	- provides the proc save_map()
		- Returns a .dmm file if map is saved
		- Returns FALSE if map fails to save

The dmm_suite provides saving and loading of map files in BYOND's native DMM map
format. It approximates the map saving and loading processes of the Dream Maker
and Dream Seeker programs so as to allow editing, saving, and loading of maps at
runtime.

------------------------

To save a map at runtime, create an instance of /dmm_suite, and then call
write_map(), which accepts three arguments:
	- A turf representing one corner of a three dimensional grid (Required).
	- Another turf representing the other corner of the same grid (Required).
	- Any, or a combination, of several bit flags (Optional, see documentation).

The order in which the turfs are supplied does not matter, the /dmm_writer will
determine the grid containing both, in much the same way as DM's block() function.
write_map() will then return a string representing the saved map in dmm format;
this string can then be saved to a file, or used for any other purose.

------------------------

To load a map at runtime, create an instance of /dmm_suite, and then call load_map(),
which accepts two arguments:
	- A .dmm file to load (Required).
	- A number representing the z-level on which to start loading the map (Optional).

The /dmm_suite will load the map file starting on the specified z-level. If no
z-level	was specified, world.maxz will be increased so as to fit the map. Note
that if you wish to load a map onto a z-level that already has objects on it,
you will have to handle the removal of those objects. Otherwise the new map will
simply load the new objects on top of the old ones.

Also note that all type paths specified in the .dmm file must exist in the world's
code, and that the /dmm_reader trusts that files to be loaded are in fact valid
.dmm files. Errors in the .dmm format will cause runtime errors.
