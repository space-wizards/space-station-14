$Path = '..\..\data\logs\' #Server directory up to the year folder, this can be a relative or absolute path; remember the trailing \
$StringToMatch = 'SQL:'
$From = 'admin@server.com'
[string[]]$To = 'email@address.com', 'a_different@address.org' #Email will be sent to each address listed here, you can have as many as you want
$Subject = 'SS13 server SQL error'
$Body = 'A SQL error was found in the following files:' #This parameter is optional, set it as '' if you want it gone
#SMTP server details; If you don't have one you can use the defaults provided here for Gmail's, provided you have a Google account
$SMTPServer = 'smtp.gmail.com'
$SMTPPort = '587'
$Account = "username" #SMTP server account name, excluding the domain address (this part: @domain.com)
$Password = 'password' #SMTP server password, if you're using Gmail's and have 2-factor authentication you'll have to use an App Password (Google for how)