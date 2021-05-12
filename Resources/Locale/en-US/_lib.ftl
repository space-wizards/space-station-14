### Special messages used by internal localizer stuff.

# Used internally by the PRESSURE() function.
zzzz-fmt-pressure = { TOSTRING($divided, "G3") } { $places ->
    [0] kPa
    [1] MPa
    [2] GPa
    [3] TPa
    [4] PBa
    *[5] ???
}

# Used internally by the THE() function.
zzzz-the = { PROPER($ent) ->
    *[false] the
     [true] {""}
    } { $ent }
