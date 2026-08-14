photograph-name-text = Это фотография { PROPER($entity) ->
    *[false] { INDEFINITE($entity) } { $entity }
    [true] { $entity }
    }.

photograph-name-text-empty = Это фотография.

photograph-name-text-photograph = Это фотография другой фотографии.
