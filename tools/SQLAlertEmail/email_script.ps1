<#
This is a script designed to parse through your server logs and locate any SQL errors reported.
If found an email is sent to addresses specified in the configuration file: email_config.ps1.
A SMTP server is required, if you don't have one the defaults for Gmail's can be used.

Suggested use is to schedule this task to be executed daily at server-time midnight so all the day's logs are checked.
You will likely find it helpful to set the configuration file to be untracked by git.
#>
. .\email_config.ps1
$Date = Get-Date -format "yyyy\\MM\\dd"
$Matches = Get-ChildItem "$Path$Date" -recurse -include *.log | Select-String "$StringToMatch" -List | Select Path, Line

$email = New-Object System.Net.Mail.MailMessage
$email.From = $From
foreach($i in $To) {$email.To.Add($i)}
$email.Subject = $Subject
$MatchList = foreach($m in $Matches) {"`t$m`n"}
$email.Body = $Body+"`n"+$MatchList

$smtp = New-Object System.Net.Mail.SmtpClient($SMTPServer, $SMTPPort);
$smtp.Credentials = New-Object System.Net.NetworkCredential($Account, $Password);
$smtp.EnableSSL = $true
$smtp.Send($email);