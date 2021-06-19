# Surgeon
surgery-step-bonegel-begin-surgeon-popup = You begin to repair the fracture in {$target}'s {$part}
surgery-step-bonegel-begin-self-surgeon-popup = You begin to repair the fracture in your {$part}
surgery-step-bonegel-begin-no-zone-surgeon-popup = You begin to repair the fracture in {PROPER($part) ->
  [false] the
  *[bucket] {""}
} {$part}

# Target
surgery-step-bonegel-begin-target-popup = {$user} begins to repair the fracture in your {$part}

# Outsider
surgery-step-bonegel-begin-outsider-popup = {$user} begins to repair the fracture in {$target}'s {$part}
surgery-step-bonegel-begin-self-outsider-popup = {$user} begins to repair the fracture in {GENDER($user) ->
  [male] his
  [female] her
  *[other] their
} {$part}
surgery-step-bonegel-begin-no-zone-outsider-popup = {$user} begins to repair the fracture in {PROPER($part) ->
  [false] the
  *[bucket] {""}
} {$part}
