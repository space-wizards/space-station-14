-air-tank-looks-like-states = { $state ->
    [notair] This air tank looks like it doesn't actually contain air.
    [regularair] This tank looks like it has regular air, which you don't breathe.
    [oxygen] This tank looks like it has oxygen, which you don't breathe.
    [nitrogen] This tank looks like it has nitrogen, which you don't breathe.
    [plasma] This tank looks like it has highly toxic plasma gas.
   *[invalid] This tank has broken the laws of reality.
}

air-tank-looks-like = {-air-tank-looks-like-states(state:$state)}

air-tank-looks-like-confirm = Use it again to breathe from the tank anyway.
