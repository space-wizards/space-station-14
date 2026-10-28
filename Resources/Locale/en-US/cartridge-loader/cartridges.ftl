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
pda-rng-badge-DivisibleBy9 = Divisible by 9
    .desc = Evenly divisible by 9.
pda-rng-badge-DivisibleBy11 = Divisible by 11
    .desc = Evenly divisible by 11.
pda-rng-badge-DivisibleBy13 = Divisible by 13
    .desc = Evenly divisible by 13.
pda-rng-badge-DivisibleBy14 = Divisible by 14
    .desc = Evenly divisible by 14.
pda-rng-badge-DivisibleBy25 = Divisible by 25
    .desc = Evenly divisible by 25.
pda-rng-badge-DivisibleBy100 = Divisible by 100
    .desc = Evenly divisible by 100.
pda-rng-badge-DivisibleBy1000 = Divisible by 1,000
    .desc = Evenly divisible by 1,000.
pda-rng-badge-Prime = Prime
    .desc = A prime number.
pda-rng-badge-Composite = Composite
    .desc = A composite number greater than 3.
pda-rng-badge-PerfectSquare = Perfect Square
    .desc = A perfect square.
pda-rng-badge-PerfectCube = Perfect Cube
    .desc = A perfect cube.
pda-rng-badge-PerfectFourth = Perfect Fourth Power
    .desc = A perfect fourth power.
pda-rng-badge-PowerOfTwo = Power of Two
    .desc = A power of two.
pda-rng-badge-Fibonacci = Fibonacci
    .desc = A Fibonacci number.
pda-rng-badge-Triangular = Triangular
    .desc = A triangular number.
pda-rng-badge-Factorial = Factorial
    .desc = A factorial number.
pda-rng-badge-PowerOfTen = Power of Ten
    .desc = A power of ten.
pda-rng-badge-PowerOfFive = Power of Five
    .desc = A power of five.
pda-rng-badge-Harshad = Harshad
    .desc = Divisible by the sum of its digits.
pda-rng-badge-DigitalRootNine = Digital Root 9
    .desc = Its digital root is 9.
pda-rng-badge-DigitalRootOne = Digital Root 1
    .desc = Its digital root is 1.
pda-rng-badge-Automorphic = Automorphic
    .desc = Its square ends with the number itself.
pda-rng-badge-Kaprekarish = Kaprekarish
    .desc = A Kaprekar-style split of its square sums back to itself.
pda-rng-badge-Repunit = Repunit
    .desc = Made entirely of ones.
pda-rng-badge-BinaryLooking = Binary Looking
    .desc = Uses only the digits 0 and 1.
pda-rng-badge-Palindrome = Palindrome
    .desc = Reads the same forwards and backwards.
pda-rng-badge-AllSameDigits = All Same Digits
    .desc = Every digit is the same.
pda-rng-badge-AllUniqueDigits = All Unique Digits
    .desc = No digit repeats.
pda-rng-badge-StrictlyAscending = Strictly Ascending
    .desc = Digits strictly increase left to right.
pda-rng-badge-StrictlyDescending = Strictly Descending
    .desc = Digits strictly decrease left to right.
pda-rng-badge-NonDecreasing = Non-Decreasing
    .desc = Digits never decrease left to right.
pda-rng-badge-NonIncreasing = Non-Increasing
    .desc = Digits never increase left to right.
pda-rng-badge-AlternatingParityDigits = Alternating Parity
    .desc = Odd and even digits alternate.
pda-rng-badge-Doublet = Doublet
    .desc = Contains two identical digits in a row.
pda-rng-badge-Triplet = Triplet
    .desc = Contains three identical digits in a row.
pda-rng-badge-Quadruplet = Quadruplet
    .desc = Contains four identical digits in a row.
pda-rng-badge-TwoPairPattern = Two Pair
    .desc = A four-digit AABB pattern with two different pairs.
pda-rng-badge-RepeatingBlock = Repeating Block
    .desc = The first half exactly repeats as the second half.
pda-rng-badge-MirrorHalves = Mirror Halves
    .desc = The second half mirrors the first half.
pda-rng-badge-ContainsZero = Contains Zero
    .desc = Contains at least one zero.
pda-rng-badge-ZeroFree = Zero-Free
    .desc = Contains no zeros.
pda-rng-badge-ContainsOne = Contains One
    .desc = Contains at least one one.
pda-rng-badge-ContainsFour = Contains Four
    .desc = Contains at least one four.
pda-rng-badge-ContainsSeven = Contains Seven
    .desc = Contains at least one seven.
pda-rng-badge-ContainsEight = Contains Eight
    .desc = Contains at least one eight.
pda-rng-badge-StartsWithOne = Starts with One
    .desc = Begins with the digit 1.
pda-rng-badge-EndsWithZero = Ends with Zero
    .desc = Ends with the digit 0.
pda-rng-badge-EndsWithSeven = Ends with Seven
    .desc = Ends with the digit 7.
pda-rng-badge-LowDigitSum = Low Digit Sum
    .desc = Digit sum is 5 or less.
pda-rng-badge-HighDigitSum = High Digit Sum
    .desc = Digit sum is 35 or more.
pda-rng-badge-DigitSumLucky7 = Digit Sum 7
    .desc = Digit sum is exactly 7.
pda-rng-badge-DigitSum14 = Digit Sum 14
    .desc = Digit sum is exactly 14.
pda-rng-badge-MostlyOddDigits = Mostly Odd Digits
    .desc = More than half of the digits are odd.
pda-rng-badge-MostlyEvenDigits = Mostly Even Digits
    .desc = Fewer than half of the digits are odd.
pda-rng-badge-OnlyOddDigits = Only Odd Digits
    .desc = Every digit is odd.
pda-rng-badge-Nice = Nice
    .desc = Exactly 69. Nice.
pda-rng-badge-BlazeIt = Blaze It
    .desc = Exactly 420.
pda-rng-badge-Beast = Beast
    .desc = Exactly 666.
pda-rng-badge-JackpotSevens = Jackpot Sevens
    .desc = Exactly 777.
pda-rng-badge-CrazyEights = Crazy Eights
    .desc = Exactly 888.
pda-rng-badge-AlmostThere = Almost There
    .desc = Exactly 999.
pda-rng-badge-Leet = Leet
    .desc = Exactly 1337.
pda-rng-badge-Boobies = Boobies
    .desc = Exactly 8008.
pda-rng-badge-BoobiesPlus = Boobies+
    .desc = Exactly 80085.
pda-rng-badge-Sequence1234 = Sequence 1234
    .desc = Exactly 1234.
pda-rng-badge-Sequence12345 = Sequence 12345
    .desc = Exactly 12345.
pda-rng-badge-NiceBlaze = Nice Blaze
    .desc = Exactly 69420.
pda-rng-badge-BlazeNice = Blaze Nice
    .desc = Exactly 42069.
pda-rng-badge-DoubleNice = Double Nice
    .desc = Exactly 6969.
pda-rng-badge-AnswerToEverything = Answer to Everything
    .desc = Exactly 42.
pda-rng-badge-NotFound = Not Found
    .desc = Exactly 404.
pda-rng-badge-OkStatus = OK Status
    .desc = Exactly 200.
pda-rng-badge-ServerError = Server Error
    .desc = Exactly 500.
pda-rng-badge-BinaryPulse = Binary Pulse
    .desc = Exactly 10101.
pda-rng-badge-HalfAndHalf = Half and Half
    .desc = Exactly 11000.
pda-rng-badge-MaxRoll = Max Roll
    .desc = Exactly 99999 — the max roll.
pda-rng-badge-ZeroHero = Zero Hero
    .desc = Exactly 0.
pda-rng-badge-UnluckyThirteen = Unlucky Thirteen
    .desc = Exactly 13.
pda-rng-badge-SpaceLawFourteen = Space Law 14
    .desc = Exactly 14.
pda-rng-badge-ThirteenThirteen = 1313
    .desc = Exactly 1313.
pda-rng-badge-FourteenFourteen = 1414
    .desc = Exactly 1414.
pda-rng-badge-FourteenHundred = Fourteen Hundred
    .desc = Exactly 1400.
pda-rng-badge-FourteenK = Fourteen K
    .desc = Exactly 14000.
pda-rng-badge-Emergency911 = Emergency 911
    .desc = Exactly 911.
pda-rng-badge-Medical99 = Medical 99
    .desc = Exactly 99.
pda-rng-badge-Contains69 = Contains 69
    .desc = Contains the digits 69.
pda-rng-badge-Contains420 = Contains 420
    .desc = Contains the digits 420.
pda-rng-badge-Contains666 = Contains 666
    .desc = Contains the digits 666.
pda-rng-badge-Contains1337 = Contains 1337
    .desc = Contains the digits 1337.
pda-rng-badge-Contains14 = Contains 14
    .desc = Contains the digits 14.
pda-rng-badge-Contains13 = Contains 13
    .desc = Contains the digits 13.
pda-rng-badge-Contains007 = Contains 007
    .desc = Contains the digits 007.
pda-rng-badge-StartsWith14 = Starts with 14
    .desc = Begins with 14.
pda-rng-badge-EndsWith14 = Ends with 14
    .desc = Ends with 14.
pda-rng-badge-StartsWith13 = Starts with 13
    .desc = Begins with 13.
pda-rng-badge-EndsWith13 = Ends with 13
    .desc = Ends with 13.
pda-rng-badge-RobustPower = Robust Power
    .desc = Exactly 256.
pda-rng-badge-RobustKilo = Robust Kilo
    .desc = Exactly 1024.
pda-rng-badge-CargoTwenty = Cargo Twenty
    .desc = Exactly 20.
pda-rng-badge-AtmosThirtyFour = Atmos 34
    .desc = Exactly 34.
pda-rng-badge-NukeAdjacent = Nuke Adjacent
    .desc = Exactly 1984.
pda-rng-badge-SingularityOne = Singularity One
    .desc = Exactly 1.
pda-rng-badge-OnlyEvenDigits = Only Even Digits
    .desc = Every digit is even.
pda-rng-badge-TernaryLooking = Ternary Looking
    .desc = Uses only the digits 0, 1, and 2.
pda-rng-badge-UsesFourDistinct = Four Distinct Digits
    .desc = Uses exactly four distinct digits.
pda-rng-badge-UsesFiveDistinct = Five Distinct Digits
    .desc = Uses exactly five distinct digits.
pda-rng-badge-UsesTwoDistinct = Two Distinct Digits
    .desc = Uses exactly two distinct digits.
pda-rng-badge-UsesThreeDistinct = Three Distinct Digits
    .desc = Uses exactly three distinct digits.
pda-rng-badge-SymmetricPairs = Symmetric Pairs
    .desc = At least two digit pairs match from both ends.
pda-rng-badge-Sandwich = Sandwich
    .desc = First and last digits match.
pda-rng-badge-Contains321 = Contains 321
    .desc = Contains the digits 321.
pda-rng-badge-Contains111 = Contains 111
    .desc = Contains the digits 111.
pda-rng-badge-Contains777 = Contains 777
    .desc = Contains the digits 777.
pda-rng-badge-Contains000 = Contains 000
    .desc = Contains the digits 000.
pda-rng-badge-StartsWith69 = Starts with 69
    .desc = Begins with 69.
pda-rng-badge-EndsWith69 = Ends with 69
    .desc = Ends with 69.
pda-rng-badge-StartsWith420 = Starts with 420
    .desc = Begins with 420.
pda-rng-badge-EndsWith420 = Ends with 420
    .desc = Ends with 420.
pda-rng-badge-LuckySevensPair = Lucky 77
    .desc = Exactly 77.
pda-rng-badge-DoctorNumber = Doctor Number
    .desc = Exactly 101.
pda-rng-badge-Gross = Gross
    .desc = Exactly 144.
pda-rng-badge-PrimeNearEnd = Prime Near End
    .desc = Exactly 999983 — a huge near-max prime.
pda-rng-badge-WizardSeven = Wizard Seven
    .desc = Exactly 7.
pda-rng-badge-SmallPrimeThree = Small Prime 3
    .desc = Exactly 3.
pda-rng-badge-SmallPrimeFive = Small Prime 5
    .desc = Exactly 5.
pda-rng-badge-BrigTime = Brig Time
    .desc = Exactly 600.
pda-rng-badge-Permabrig = Permabrig
    .desc = Exactly 9999.
pda-rng-badge-HereticMark = Heretic Mark
    .desc = Exactly 66666.
pda-rng-badge-DivisibleBy15 = Divisible by 15
    .desc = Evenly divisible by 15.
pda-rng-badge-DivisibleBy16 = Divisible by 16
    .desc = Evenly divisible by 16.
pda-rng-badge-DivisibleBy17 = Divisible by 17
    .desc = Evenly divisible by 17.
pda-rng-badge-DivisibleBy21 = Divisible by 21
    .desc = Evenly divisible by 21.
pda-rng-badge-DivisibleBy42 = Divisible by 42
    .desc = Evenly divisible by 42.
pda-rng-badge-DivisibleBy69 = Divisible by 69
    .desc = Evenly divisible by 69.
pda-rng-badge-PerfectFifth = Perfect Fifth Power
    .desc = A perfect fifth power.
pda-rng-badge-PowerOfThree = Power of Three
    .desc = A power of three.
pda-rng-badge-SparseDigits = Sparse Digits
    .desc = Digit sum is at most twice the digit count.
pda-rng-badge-DigitSumEqualsLength = Digit Sum Equals Length
    .desc = Digit sum equals the number of digits.
pda-rng-badge-StartsAndEndsSamePair = Bookend Pair
    .desc = Starts and ends with the same two digits.
pda-rng-badge-CenteredDoublet = Centered Doublet
    .desc = The middle two digits match.
pda-rng-badge-NoConsecutiveDigits = No Doubles
    .desc = No two identical digits are adjacent.
pda-rng-badge-HasAscendingPair = Ascending Pair
    .desc = Contains two adjacent digits that rise by one.
pda-rng-badge-HasDescendingPair = Descending Pair
    .desc = Contains two adjacent digits that fall by one.
pda-rng-badge-ContainsConsecutiveRun3 = Triple Run
    .desc = Contains three consecutive ascending or descending digits.
pda-rng-badge-FirstDigitEven = Even Lead
    .desc = The first digit is even.
pda-rng-badge-LastDigitOdd = Odd Finish
    .desc = The last digit is odd.
pda-rng-badge-AllDigitsLessThanFive = Low Digits
    .desc = Every digit is 0 through 4.
pda-rng-badge-AllDigitsAtLeastFive = High Digits
    .desc = Every digit is 5 through 9.
pda-rng-badge-AlternatingSamePair = Alternating Pair
    .desc = Alternates between two different digits.
pda-rng-badge-MirrorEnds = Mirror Ends
    .desc = The first two digits mirror the last two.
pda-rng-badge-IncreasingPairs = Increasing Pairs
    .desc = Consecutive digit pairs form increasing two-digit numbers.
pda-rng-badge-BalancedParityDigits = Balanced Parity
    .desc = Exactly half the digits are odd.
pda-rng-badge-OverNineThousand = Over Nine Thousand
    .desc = Exactly 9001.
pda-rng-badge-BinaryMaxByte = Max Byte
    .desc = Exactly 255.
pda-rng-badge-KilobyteAdjacent = Kilobyte Adjacent
    .desc = Exactly 1023.
pda-rng-badge-Year2000 = Year 2000
    .desc = Exactly 2000.
pda-rng-badge-Year2024 = Year 2024
    .desc = Exactly 2024.
pda-rng-badge-Lucky888 = Lucky 8888
    .desc = Exactly 8888.
pda-rng-badge-UnluckyFourFour = Unlucky 4444
    .desc = Exactly 4444.
pda-rng-badge-SecurityTwentyFive = Security 25
    .desc = Exactly 25.
pda-rng-badge-ClownHonk = Clown Honk
    .desc = Exactly 360.
pda-rng-badge-CaptainOneFive = Captain 15
    .desc = Exactly 15.
pda-rng-badge-EngineerThirty = Engineer 30
    .desc = Exactly 30.
pda-rng-badge-LeapDay = Leap Day
    .desc = Exactly 229.
pda-rng-badge-Halloween = Halloween
    .desc = Exactly 1031.
pda-rng-badge-Contains88 = Contains 88
    .desc = Contains the digits 88.
pda-rng-badge-Contains99 = Contains 99
    .desc = Contains the digits 99.
pda-rng-badge-Contains123 = Contains 123
    .desc = Contains the digits 123.
pda-rng-badge-Contains7777 = Contains 7777
    .desc = Contains the digits 7777.
pda-rng-badge-Contains42 = Contains 42
    .desc = Contains the digits 42.
pda-rng-badge-Contains911 = Contains 911
    .desc = Contains the digits 911.
pda-rng-badge-StartsWith77 = Starts with 77
    .desc = Begins with 77.
pda-rng-badge-EndsWith77 = Ends with 77
    .desc = Ends with 77.
pda-rng-badge-StartsWith88 = Starts with 88
    .desc = Begins with 88.
pda-rng-badge-EndsWith88 = Ends with 88
    .desc = Ends with 88.
pda-rng-badge-StartsWith99 = Starts with 99
    .desc = Begins with 99.
pda-rng-badge-EndsWith99 = Ends with 99
    .desc = Ends with 99.
pda-rng-badge-SixtySeven = Sixty-Seven
    .desc = Exactly 67.
pda-rng-badge-Contains67 = Contains 67
    .desc = Contains the digits 67.
pda-rng-badge-ConsecutiveDigram = Consecutive Pair
    .desc = A two-digit block repeats back-to-back (like 7373).

pda-rng-badge-Sequence54321 = Sequence 54321
    .desc = Exactly 54321.
pda-rng-badge-PiLite = Pi Lite
    .desc = Exactly 31415 — pi truncated to five digits.
pda-rng-badge-EulerLite = Euler Lite
    .desc = Exactly 27182 — e truncated to five digits.
pda-rng-badge-JennyLite = Jenny Lite
    .desc = Exactly 86753.
pda-rng-badge-ThirteenCascade = Thirteen Cascade
    .desc = Exactly 13131.
pda-rng-badge-FourteenCascade = Fourteen Cascade
    .desc = Exactly 14141.
pda-rng-badge-ThirteenFourteen = Thirteen-Fourteen
    .desc = Exactly 1314 — unlucky meets space law.
pda-rng-badge-Mirror12321 = Mirror 12321
    .desc = Exactly 12321.
pda-rng-badge-AllFives = All Fives
    .desc = Exactly 55555.
pda-rng-badge-QuadSevens = Quad Sevens
    .desc = Exactly 77777.
pda-rng-badge-Repeater12121 = Repeater 12121
    .desc = Exactly 12121.
pda-rng-badge-NinetyK = Ninety K
    .desc = Exactly 90000.
pda-rng-badge-FiftyK = Fifty K
    .desc = Exactly 50000.
