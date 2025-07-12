-air-tank-looks-like-states = { $state ->
    [notair] This probably doesn't actually contain air.
    [regularair] This probably has regular air, which you don't breathe.
    [oxygen] This probably has oxygen, which you don't breathe.
    [nitrogen] This probably has nitrogen, which you don't breathe.
    [plasma] This looks toxic.
   *[invalid] This tank has broken the laws of reality.
}

air-tank-looks-like = {-air-tank-looks-like-states(state:$state)}
air-tank-looks-like-examine = {-air-tank-looks-like-examine-states(state:$state)}
air-tank-looks-like-dont = You shouldn't breathe from this.

air-tank-looks-like-confirm = Use it again to breathe from the tank anyway.
