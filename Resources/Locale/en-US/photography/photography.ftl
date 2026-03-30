# TODO: Make this a fluent function in RT
photograph-description = This is a photograph of { PROPER($entity) ->
    *[false] { INDEFINITE($entity) } { $entity }
     [true] { $entity }
    }
photograph-description-empty = This is a photograph.
photograph-description-recursive = This is a photograph of another photograph.
