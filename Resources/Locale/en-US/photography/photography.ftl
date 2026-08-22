# TODO: Make this a fluent function in RT
photograph-name-text = a photograph of { PROPER($entity) ->
    *[false] { INDEFINITE($entity) } { $entity }
     [true] { $entity }
    }
photograph-name-text-empty = a photograph
photograph-name-text-photograph = a photograph of another photograph

photograph-examine = This is {$text}.
photograph-label-examine = It is {$text}.
