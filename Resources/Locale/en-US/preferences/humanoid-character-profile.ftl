### UI

# Displayed in the Character prefs window
humanoid-character-profile-summary =
    This is {$name}. {$subject ->
    [male] He
    [female] She
    [epicene] They
    [neuter] It
    *[other] {$subject}
} {$conjugate-be ->
    [male] is
    [female] is
    [neuter] is
    [false] is
    *[other] {$conjugate-be}
} {$age} years old.
