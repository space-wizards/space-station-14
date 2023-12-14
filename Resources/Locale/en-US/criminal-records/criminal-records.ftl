criminal-records-console-window-title = Criminal Records Computer
criminal-records-console-select-record-info = Select a record on the left.
criminal-records-console-empty-state = No records found!
criminal-records-console-no-record-found = No record was found for the selected person.

criminal-records-arrest-button = Arrest
criminal-records-release-button = Release

criminal-records-reason-placeholder = Reason

criminal-records-permission-denied = Permission denied

criminal-records-console-record-age = Age: {$age}
criminal-records-console-record-title = Job: {$job}
criminal-records-console-record-species = Species: {$species}
criminal-records-console-record-gender = Gender: {$gender}
criminal-records-console-record-fingerprint = Fingerprint: {$fingerprint}
criminal-records-console-record-dna = DNA: {$dna}
criminal-records-console-record-status = Status: {$status ->
    *[none]    None
    [wanted]   [color=red]Wanted[/color]
    [detained] [color=dodgerblue]Detained[/color]
}

## Security channel notifications
### On Arrest/Release button pressed

criminal-records-console-detained = {$name} has been detained {$hasReason ->
    *[zero] by {$officer}
    [other] by {$officer} with reason: {$reason}
}

criminal-records-console-released = {$name} has been released from the detention {$hasReason ->
    *[zero] by {$officer}
    [other] by {$officer} with reason: {$reason}
}

### On status changed

criminal-records-console-wanted = {$name} is wanted {$hasReason ->
    *[zero] by {$officer}
    [other] by {$officer} with reason: {$reason}
}

criminal-records-console-not-wanted = {$name} is not wanted anymore {$hasReason ->
    *[zero] by {$officer}
    [other] by {$officer} with reason: {$reason}
}

## Filters

criminal-records-filter-placeholder = Input text and press "Enter"
criminal-records-name-filter = Name of person
criminal-records-prints-filter = Fingerprints
criminal-records-dna-filter = DNA
criminal-records-console-search-records = Search
criminal-records-console-reset-filters = Reset
