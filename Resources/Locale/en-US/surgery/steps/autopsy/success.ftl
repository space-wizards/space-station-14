# Surgeon
surgery-step-autopsy-success-surgeon-popup = You perform an autopsy on {$target}'s {$part}
surgery-step-autopsy-success-self-surgeon-popup = You perform an autopsy on your {$part}
surgery-step-autopsy-success-no-zone-surgeon-popup = You perform an autopsy on {PROPER($part) ->
  [false] the
  *[bucket] {""}
} {$part}

# Target
surgery-step-autopsy-success-target-popup = {$user} performs an autopsy on your {$part}

# Outsider
surgery-step-autopsy-success-outsider-popup = {$user} performs an autopsy on {$target}'s {$part}
surgery-step-autopsy-success-self-outsider-popup = {$user} performs an autopsy on {GENDER($user) ->
  [male] his
  [female] her
  *[other] their
} {$part}
surgery-step-autopsy-success-no-zone-outsider-popup = {$user} perform an autopsy on {PROPER($part) ->
  [false] the
  *[bucket] {""}
} {$part}
