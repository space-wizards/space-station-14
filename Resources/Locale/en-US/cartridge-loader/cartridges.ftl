device-pda-slot-component-slot-name-cartridge = Cartridge

default-program-name = Program
notekeeper-program-name = Notekeeper
nano-task-program-name = NanoTask
news-read-program-name = Station news

crew-manifest-program-name = Crew manifest
crew-manifest-cartridge-loading = Loading ...
crew-manifest-cartridge-loading-failed = Failed to load crew manifest!

net-probe-program-name = NetProbe
net-probe-scan = Scanned {$device}!
net-probe-label-name = Name
net-probe-label-address = Address
net-probe-label-frequency = Frequency
net-probe-label-network = Network

log-probe-program-name = LogProbe
log-probe-scan = Downloaded logs from {$device}!
log-probe-label-time = Time
log-probe-label-accessor = Accessed by
log-probe-label-number = #
log-probe-print-button = Print Logs
log-probe-printout-device = Scanned Device: {$name}
log-probe-printout-header = Latest logs:
log-probe-printout-entry = #{$number} / {$time} / {$accessor}

astro-nav-program-name = AstroNav

med-tek-program-name = MedTek

# NanoTask cartridge

nano-task-ui-heading-high-priority-tasks =
    { $amount ->
        [zero] No High Priority Tasks
        [one] 1 High Priority Task
       *[other] {$amount} High Priority Tasks
    }
nano-task-ui-heading-medium-priority-tasks =
    { $amount ->
        [zero] No Medium Priority Tasks
        [one] 1 Medium Priority Task
       *[other] {$amount} Medium Priority Tasks
    }
nano-task-ui-heading-low-priority-tasks =
    { $amount ->
        [zero] No Low Priority Tasks
        [one] 1 Low Priority Task
       *[other] {$amount} Low Priority Tasks
    }
nano-task-ui-done = Done
nano-task-ui-revert-done = Undo
nano-task-ui-priority-low = Low
nano-task-ui-priority-medium = Medium
nano-task-ui-priority-high = High
nano-task-ui-cancel = Cancel
nano-task-ui-print = Print
nano-task-ui-delete = Delete
nano-task-ui-save = Save
nano-task-ui-new-task = New Task
nano-task-ui-description-label = Description:
nano-task-ui-description-placeholder = Get something important
nano-task-ui-requester-label = Requester:
nano-task-ui-requester-placeholder = John Nanotrasen
nano-task-ui-item-title = Edit Task
nano-task-printed-description = [bold]Description[/bold]: {$description}
nano-task-printed-requester = [bold]Requester[/bold]: {$requester}
nano-task-printed-high-priority = [bold]Priority[/bold]: [color=red]High[/color]
nano-task-printed-medium-priority = [bold]Priority[/bold]: Medium
nano-task-printed-low-priority = [bold]Priority[/bold]: Low

# Wanted list cartridge
wanted-list-program-name = Wanted list
wanted-list-label-no-records = It's all right, cowboy
wanted-list-search-placeholder = Search by name and status

wanted-list-age-label = [color=darkgray]Age:[/color] [color=white]{$age}[/color]
wanted-list-job-label = [color=darkgray]Job:[/color] [color=white]{$job}[/color]
wanted-list-species-label = [color=darkgray]Species:[/color] [color=white]{$species}[/color]
wanted-list-gender-label = [color=darkgray]Gender:[/color] [color=white]{$gender}[/color]

wanted-list-reason-label = [color=darkgray]Reason:[/color] [color=white]{$reason}[/color]
wanted-list-unknown-reason-label = unknown reason

wanted-list-initiator-label = [color=darkgray]Initiator:[/color] [color=white]{$initiator}[/color]
wanted-list-unknown-initiator-label = unknown initiator

wanted-list-status-label = [color=darkgray]status:[/color] {$status ->
        [suspected] [color=yellow]suspected[/color]
        [wanted] [color=red]wanted[/color]
        [detained] [color=#b18644]detained[/color]
        [paroled] [color=green]paroled[/color]
        [discharged] [color=green]discharged[/color]
        [hostile] [color=darkred]hostile[/color]
        [eliminated] [color=gray]eliminated[/color]
        *[other] none
    }

wanted-list-history-table-time-col = Time
wanted-list-history-table-reason-col = Crime
wanted-list-history-table-initiator-col = Initiator

# PDA RNG cartridge
pda-rng-program-name = PDA RNG
pda-rng-ui-description = Generate one random number ticket this shift. Results are printed on paper.
pda-rng-ui-print-button = Print Ticket
pda-rng-ui-status-ready = Ready — one roll remaining.
pda-rng-ui-status-rolled = Already rolled this shift.
pda-rng-already-rolled = This PDA has already printed its RNG ticket this shift.

pda-rng-tier-mundane = Mundane
pda-rng-tier-notable = Notable
pda-rng-tier-irregular = Irregular
pda-rng-tier-anomalous = Anomalous
pda-rng-tier-classified = Classified
pda-rng-tier-reality-bend = Reality-Bend
pda-rng-tier-singularity = Singularity

pda-rng-printout-header = [head=2][color=#3d6ea5]- PDA RNG RESULT -[/color][/head]
pda-rng-printout-number = [head=1]{$number}[/head]
pda-rng-printout-tier-top = [bold]Tier:[/bold] [color={$color}][head=3]{$tier}[/head][/color]  ·  [color=#666666]Top {$percentile}%[/color]
pda-rng-printout-tier-bottom = [bold]Tier:[/bold] [color={$color}]{$tier}[/color]  ·  [color=#666666]Bottom {$percentile}%[/color]
pda-rng-printout-total-xp = [bold]Total XP:[/bold] [color=#2e7d32]{$xp}[/color]
pda-rng-printout-flavor = [color=#666666][italic]{$digits} digits · digit sum {$sum} · {$unique} unique · {$odd} odd / {$even} even[/italic][/color]
pda-rng-printout-badges-header = [head=3]Badges[/head]
pda-rng-printout-badge-entry = [color=#c9a227]★[/color] [bold]{$name}[/bold] [color=#2e7d32](+{$xp} XP)[/color] — [color=#555555]{$desc}[/color]
pda-rng-printout-badges-overflow = [color=#666666][italic]+ {$count} more badges ({$xp} XP)[/italic][/color]
pda-rng-printout-badges-none = [color=#666666][italic]— None —[/italic][/color]
pda-rng-printout-issued-to = [color=#444444]Issued to:[/color] [bold]{$name}[/bold]
pda-rng-printout-label = RNG {$number}

# Badges
pda-rng-badge-OneDigit = One Digit
    .desc = Rolled a single-digit number.
pda-rng-badge-TwoDigits = Two Digits
    .desc = Rolled a two-digit number.
pda-rng-badge-ThreeDigits = Three Digits
    .desc = Rolled a three-digit number.
pda-rng-badge-FourDigits = Four Digits
    .desc = Rolled a four-digit number.
pda-rng-badge-FiveDigits = Five Digits
    .desc = Rolled a full five-digit number.
pda-rng-badge-Even = Even
    .desc = The number is even.
pda-rng-badge-Odd = Odd
    .desc = The number is odd.
pda-rng-badge-DivisibleBy3 = Divisible by 3
    .desc = Evenly divisible by 3.
pda-rng-badge-DivisibleBy5 = Divisible by 5
    .desc = Evenly divisible by 5.
pda-rng-badge-DivisibleBy7 = Divisible by 7
    .desc = Evenly divisible by 7.
pda-rng-badge-DivisibleBy13 = Divisible by 13
    .desc = Evenly divisible by 13.
pda-rng-badge-DivisibleBy14 = Divisible by 14
    .desc = Evenly divisible by 14.
pda-rng-badge-Prime = Prime
    .desc = A prime number.
pda-rng-badge-PerfectSquare = Perfect Square
    .desc = A perfect square.
pda-rng-badge-PerfectCube = Perfect Cube
    .desc = A perfect cube.
pda-rng-badge-Fibonacci = Fibonacci
    .desc = A Fibonacci number.
pda-rng-badge-PowerOfTwo = Power of Two
    .desc = A power of two.
pda-rng-badge-Palindrome = Palindrome
    .desc = Reads the same forwards and backwards.
pda-rng-badge-AllSameDigits = All Same Digits
    .desc = Every digit is the same.
pda-rng-badge-AllUniqueDigits = All Unique Digits
    .desc = No digit repeats.
pda-rng-badge-StrictlyAscending = Strictly Ascending
    .desc = Each digit is larger than the last.
pda-rng-badge-StrictlyDescending = Strictly Descending
    .desc = Each digit is smaller than the last.
pda-rng-badge-Doublet = Doublet
    .desc = Contains two identical digits in a row.
pda-rng-badge-Triplet = Triplet
    .desc = Contains three identical digits in a row.
pda-rng-badge-RepeatingBlock = Repeating Block
    .desc = The first half equals the second half.
pda-rng-badge-ConsecutiveDigram = Consecutive Pair
    .desc = A two-digit block repeats back-to-back.
pda-rng-badge-BinaryLooking = Binary Looking
    .desc = Uses only the digits 0 and 1.
pda-rng-badge-ZeroFree = Zero-Free
    .desc = Contains no zeros.
pda-rng-badge-Sandwich = Sandwich
    .desc = Starts and ends with the same digit.
pda-rng-badge-ZeroHero = Zero Hero
    .desc = Exactly 0.
pda-rng-badge-SingularityOne = Singularity One
    .desc = Exactly 1.
pda-rng-badge-UnluckyThirteen = Unlucky Thirteen
    .desc = Exactly 13.
pda-rng-badge-SpaceLawFourteen = Space Law 14
    .desc = Exactly 14.
pda-rng-badge-AnswerToEverything = Answer to Everything
    .desc = Exactly 42.
pda-rng-badge-SixtySeven = Sixty-Seven
    .desc = Exactly 67.
pda-rng-badge-Nice = Nice
    .desc = Exactly 69.
pda-rng-badge-BlazeIt = Blaze It
    .desc = Exactly 420.
pda-rng-badge-Beast = Beast
    .desc = Exactly 666.
pda-rng-badge-JackpotSevens = Jackpot Sevens
    .desc = Exactly 777.
pda-rng-badge-Emergency911 = Emergency 911
    .desc = Exactly 911.
pda-rng-badge-ThirteenThirteen = 1313
    .desc = Exactly 1313.
pda-rng-badge-FourteenFourteen = 1414
    .desc = Exactly 1414.
pda-rng-badge-Leet = Leet
    .desc = Exactly 1337.
pda-rng-badge-NukeAdjacent = Nuke Adjacent
    .desc = Exactly 1984.
pda-rng-badge-Sequence12345 = Sequence 12345
    .desc = Exactly 12345.
pda-rng-badge-ThirteenCascade = Thirteen Cascade
    .desc = Exactly 13131.
pda-rng-badge-FourteenCascade = Fourteen Cascade
    .desc = Exactly 14141.
pda-rng-badge-FourteenK = Fourteen K
    .desc = Exactly 14000.
pda-rng-badge-PiLite = Pi Lite
    .desc = Exactly 31415 — pi truncated to five digits.
pda-rng-badge-Sequence54321 = Sequence 54321
    .desc = Exactly 54321.
pda-rng-badge-AllFives = All Fives
    .desc = Exactly 55555.
pda-rng-badge-HereticMark = Heretic Mark
    .desc = Exactly 66666.
pda-rng-badge-NiceBlaze = Nice Blaze
    .desc = Exactly 69420.
pda-rng-badge-BlazeNice = Blaze Nice
    .desc = Exactly 42069.
pda-rng-badge-QuadSevens = Quad Sevens
    .desc = Exactly 77777.
pda-rng-badge-FunnyNumber = Funny Number
    .desc = Exactly 80085.
pda-rng-badge-PrimeNearEnd = Prime Near End
    .desc = Exactly 99991 — the largest five-digit prime.
pda-rng-badge-MaxRoll = Max Roll
    .desc = Exactly 99999 — the max roll.
pda-rng-badge-Contains13 = Contains 13
    .desc = Contains the digits 13.
pda-rng-badge-Contains14 = Contains 14
    .desc = Contains the digits 14.
pda-rng-badge-Contains67 = Contains 67
    .desc = Contains the digits 67.
pda-rng-badge-Contains69 = Contains 69
    .desc = Contains the digits 69.
pda-rng-badge-Contains420 = Contains 420
    .desc = Contains the digits 420.
pda-rng-badge-Contains666 = Contains 666
    .desc = Contains the digits 666.
pda-rng-badge-Contains777 = Contains 777
    .desc = Contains the digits 777.
pda-rng-badge-Contains007 = Contains 007
    .desc = Contains the digits 007.
pda-rng-badge-StartsWith13 = Starts with 13
    .desc = Begins with 13.
pda-rng-badge-EndsWith13 = Ends with 13
    .desc = Ends with 13.
pda-rng-badge-StartsWith14 = Starts with 14
    .desc = Begins with 14.
pda-rng-badge-EndsWith14 = Ends with 14
    .desc = Ends with 14.
pda-rng-badge-StartsWith69 = Starts with 69
    .desc = Begins with 69.
pda-rng-badge-EndsWith69 = Ends with 69
    .desc = Ends with 69.
