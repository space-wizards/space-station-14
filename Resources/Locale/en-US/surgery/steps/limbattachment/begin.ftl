# Surgeon
surgery-step-limbattachment-begin-surgeon-popup = You begin to attach {$target}'s {$part}
surgery-step-limbattachment-begin-self-surgeon-popup = You begin to attach your {$part}
surgery-step-limbattachment-begin-no-zone-surgeon-popup = You begin to attach {PROPER($part) ->
  [false] the
  *[bucket] {""}
} {$part}

# Target
surgery-step-limbattachment-begin-target-popup = {$user} begins to attach your {$part}

# Outsider
surgery-step-limbattachment-begin-outsider-popup = {$user} begins to attach {$target}'s {$part}
surgery-step-limbattachment-begin-self-outsider-popup = {$user} begins to attach {GENDER($user) ->
  [male] his
  [female] her
  *[other] their
} {$part}
surgery-step-limbattachment-begin-no-zone-outsider-popup = {$user} begins to attach {PROPER($part) ->
  [false] the
  *[bucket] {""}
} {$part}
