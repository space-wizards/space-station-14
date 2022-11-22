## File

# Title of the file menu.
mapping-menus-file = File

# Menu entry for creating a new map/file.
mapping-menus-file-new = New

# Menu entry for opening an existing map/file.
mapping-menus-file-open = Open

# Menu entry for exiting mapping mode and returning to normal play.
mapping-menus-file-exit-mapping-scene = Play mode

# Menu entry for disconnecting the client from the server.
mapping-menus-file-disconnect = Disconnect

# Menu entry for quitting, closing both the client and server.
mapping-menus-file-quit = Quit

## Visibility

# Title of the visibility menu.
mapping-menus-file-visibility = Visibility

# Toggleable menu entry for marker entity visibility.
mapping-menus-visibility-markers =
    Markers ({ $value ->
        [true] on
        *[false] off
    })
# Toggleable menu entry for subfloor entity visibility.
mapping-menus-visibility-subfloor =
    Subfloor ({ $value ->
        [true] on
        *[false] off
    })
