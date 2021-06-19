# Surgeon
surgery-step-autopsy-begin-surgeon-popup = You begin an autopsy on {$target}'s {$part}
surgery-step-autopsy-begin-self-surgeon-popup = You begin an autopsy on your {$part}
surgery-step-autopsy-begin-no-zone-surgeon-popup = You begin an autopsy on {PROPER($part) ->
  [false] the
  *[bucket] {""}
} {$part}

# Target
surgery-step-autopsy-begin-target-popup = {$user} begins an autopsy on your {$part}

# Outsider
surgery-step-autopsy-begin-outsider-popup = {$user} begins an autopsy on {$target}'s {$part}
surgery-step-autopsy-begin-self-outsider-popup = {$user} begins an autopsy on {GENDER($user) ->
  [male] his
  [female] her
  *[other] their
} {$part}
surgery-step-autopsy-begin-no-zone-outsider-popup = {$user} begins an autopsy on {PROPER($part) ->
  [false] the
  *[bucket] {""}
} {$part}
