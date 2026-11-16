# TODO: Make this a fluent function in RT
photograph-name-text = This is a photograph of { PROPER($entity) ->
    *[false] { INDEFINITE($entity) } { $entity }
     [true] { $entity }
    }.
photograph-name-text-empty = This is a photograph.
photograph-name-text-photograph = This is a photograph of another photograph.
