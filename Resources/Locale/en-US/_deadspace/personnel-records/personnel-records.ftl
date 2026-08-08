# Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

personnel-records-console-permission-denied = Access denied.
personnel-records-console-unknown-officer = <unknown>
personnel-records-console-job-assignment-blocked = That position's slot is already filled. Central Command access required.

## Order printing (PersonnelPrintingSystem)

paperwork-form-title-personnel-discipline = Disciplinary Order
personnel-records-print-sanction-reprimand = reprimand
personnel-records-print-sanction-demotion = demotion
personnel-records-print-sanction-dismissal = dismissal
personnel-records-print-unknown-department = an unknown department

## Radio announcements - target's department

personnel-records-console-announce-reprimand = { $name } ({ $job }) has been issued a reprimand. Reason: { $reason }. Issued by { $officer }.
personnel-records-console-announce-demotion = { $name } ({ $job }) has been marked for demotion. Reason: { $reason }. Issued by { $officer }.
personnel-records-console-announce-dismissal = { $name } ({ $job }) has been marked for dismissal. Reason: { $reason }. Issued by { $officer }.
personnel-records-console-announce-annul = The order against { $name } ({ $job }) has been annulled. No escort required. Reason: { $reason }. Issued by { $officer }.

## Radio announcements - Security channel

personnel-records-console-announce-security-demotion = { $name } ({ $job }) has been marked for demotion. Escort to the Head of Personnel. Issued by { $officer }.
personnel-records-console-announce-security-dismissal = { $name } ({ $job }) has been marked for dismissal. Escort to the Head of Personnel. Issued by { $officer }.
personnel-records-console-announce-annul-security = The order against { $name } ({ $job }) has been annulled. No escort required. Reason: { $reason }. Issued by { $officer }.

## Radio announcements - order executed (PersonnelOrderCompletionSystem, no officer attached)

personnel-records-console-announce-executed = The order against { $name } has been carried out. No escort required.
personnel-records-console-announce-executed-security = The order against { $name } has been carried out. No escort required.

## Criminal record history lines (PersonnelSecurityBridgeSystem)

personnel-records-criminal-history-demotion = HR: marked for demotion. Reason: { $reason }.
personnel-records-criminal-history-dismissal = HR: marked for dismissal. Reason: { $reason }.
personnel-records-criminal-history-annulled = HR: order annulled. Reason: { $reason }.
personnel-records-criminal-history-executed = HR: order executed.

## Console window

personnel-records-console-window-title = Personnel Records Console
personnel-records-console-records-list-title = Crew members
personnel-records-console-select-record-info = Select a record.
personnel-records-console-no-records = No records found!
personnel-records-console-no-department = Department not determined.
personnel-records-console-show-all = All

## Employment status

personnel-records-console-status = Employment status
personnel-records-status-none = No orders
personnel-records-status-reprimand = Reprimand
personnel-records-status-demotion = Demotion
personnel-records-status-dismissal = Dismissal

personnel-records-console-reason-label = [color=gray]Reason[/color]
personnel-records-console-initiator-label = [color=gray]Issued by[/color]
personnel-records-console-criminal-status = Criminal status

## Buttons

personnel-records-console-reprimand-button = Reprimand
personnel-records-console-demote-button = Demote
personnel-records-console-dismiss-button = Dismiss
personnel-records-console-annul-button = Annul order
personnel-records-console-print-button = Print order
personnel-records-console-declare-wanted-button = Declare wanted
personnel-records-console-history-button = Order history

## Reason dialogs

personnel-records-console-reason = Reason
personnel-records-console-reason-placeholder = Describe the reason for this order
personnel-records-console-annul-reason-placeholder = Describe the reason for cancelling this order
personnel-records-console-wanted-reason-placeholder = Describe the reason for the wanted status

## Crew member card

personnel-records-console-record-department = Department: { $department }

## Filters

personnel-records-filter-placeholder = Enter text and press "Enter"
personnel-records-name-filter = Name
personnel-records-prints-filter = Fingerprint
personnel-records-dna-filter = DNA
personnel-records-job-filter = Job
personnel-records-species-filter = Species

## History window

personnel-records-history-window-title = Order history
personnel-records-no-history = This crew member has no personnel order history.
personnel-records-history-type-reprimand = Reprimand
personnel-records-history-type-demotion = Demotion
personnel-records-history-type-dismissal = Dismissal
personnel-records-history-type-annul = Order annulled
personnel-records-history-type-executed = Order executed
personnel-records-history-auto-executed = Order automatically closed: job changed to "{ $job }".

## ID card console "Dismiss" button (PersonnelDismissalSystem)

personnel-dismissal-button = Dismiss
personnel-dismissal-confirm = Are you sure?
personnel-dismissal-permission-denied = Access denied.
personnel-dismissal-no-record = This card has no personnel record.
personnel-dismissal-not-issued = No dismissal order has been issued.
