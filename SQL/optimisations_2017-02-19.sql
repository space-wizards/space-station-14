/*
It is recommended you do not perfom any of these queries while your server is live as they will lock the tables during execution.

Backup your database before starting; Breaking errors may occur when old data is not be compatible with new column formats.
i.e. A field that is null or empty due to rows existing before the column was added, data corruption or incorrect inputs will prevent altering the column to be NOT NULL.
To account where this is likely to occur, these queries check if fields have valid data and if not will replace invalid data with 0.
Some fields may be out of range for new column lengths, this will also cause a breaking error.
For instance `death`.`bruteloss` is now SMALLINT and won't accept any values over 65535.
Data in this table can thus be truncated using a query such as:
UPDATE `[database]`.`[table]` SET `[column]` = LEAST(`[column]`, [max column size])
To truncate a text field you would have to use SUBSTRING(), however I don't suggest you truncate any text fields.
If you wish to instead preserve this data you will need to modify the schema and queries to accomodate.

Additionally, you may encounter the error "Error Code: 1411. Incorrect string value: '[query]' for function inet_aton".
This is due to a bug with the default sql_mode in MySQL version 5.7, wherein a value of '' cannot be passed to INET_ATON() without error, see here for reference: https://bugs.mysql.com/bug.php?id=82280
The INET_ATON() function inteprets any abnormal data as being '', examples are:
' 127.0.0.1' - contains a whitespace character such as space, tab or newline.
'127.a.$,1' - contains non-numeric characters
'300.0.0.1' - ipv4 octets must be within a range of 0 to 255
Note that '127.0.1' and '127.0..1' are both valid data formats but will not equate to the correct ip address.

If you get this error there are steps you can take to deal with the invalid data:
To get a list of all unique fields that aren't valid ipv4 addresses, run the query:
SELECT DISTINCT [column] FROM [table] WHERE [column] NOT REGEXP '^(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$'
Note this will only retrieve the first 1000 results, if you want more append to the query:
LIMIT 0, [max result]
Now inspect your results for any data that is invalid and correct them.
If you prefer, you can also automatically replace invalid data with 0 using a similar query:
UPDATE [table] SET [column] = '0' WHERE [column] NOT REGEXP '^(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$'
This will replace all invalid data, even data that could potentially be corrected.
Finally, per the bug report, you can have MySQL ignore the errors and instead produce warnings while continuing with the query.
To do so, run the query:
SET @@sql_mode=''
Do not use this method unless you are sure about it; This setting will persist until your MySQL server is restarted, potentially affecting the data integrity of this and future queries.

Take note some columns have been renamed, removed or changed type. Any services relying on these columns will have to be updated per changes.

----------------------------------------------------*/

START TRANSACTION;
ALTER TABLE `ban`
 DROP COLUMN `rounds`
, CHANGE COLUMN `bantype` `bantype` ENUM('PERMABAN', 'TEMPBAN', 'JOB_PERMABAN', 'JOB_TEMPBAN', 'ADMIN_PERMABAN', 'ADMIN_TEMPBAN') NOT NULL
, CHANGE COLUMN `reason` `reason` VARCHAR(2048) NOT NULL
, CHANGE COLUMN `who` `who` VARCHAR(2048) NOT NULL
, CHANGE COLUMN `adminwho` `adminwho` VARCHAR(2048) NOT NULL
, CHANGE COLUMN `unbanned` `unbanned` TINYINT UNSIGNED NULL DEFAULT NULL
, ADD COLUMN `server_ip` INT UNSIGNED NOT NULL AFTER `serverip`
, ADD COLUMN `server_port` SMALLINT UNSIGNED NOT NULL AFTER `server_ip`
, ADD COLUMN `ipTEMP` INT UNSIGNED NOT NULL AFTER `ip`
, ADD COLUMN `a_ipTEMP` INT UNSIGNED NOT NULL AFTER `a_ip`
, ADD COLUMN `unbanned_ipTEMP` INT UNSIGNED NULL DEFAULT NULL AFTER `unbanned_ip`;
SET SQL_SAFE_UPDATES = 0;
UPDATE `ban`
 SET `server_ip` = INET_ATON(SUBSTRING_INDEX(IF(`serverip` = '', '0', IF(SUBSTRING_INDEX(`serverip`, ':', 1) LIKE '%_._%', `serverip`, '0')), ':', 1))
, `server_port` = IF(`serverip` LIKE '%:_%', CAST(SUBSTRING_INDEX(`serverip`, ':', -1) AS UNSIGNED), '0')
, `ipTEMP` = INET_ATON(IF(`ip` LIKE '%_._%', `ip`, '0'))
, `a_ipTEMP` = INET_ATON(IF(`a_ip` LIKE '%_._%', `a_ip`, '0'))
, `unbanned_ipTEMP` = INET_ATON(IF(`unbanned_ip` LIKE '%_._%', `unbanned_ip`, '0'));
SET SQL_SAFE_UPDATES = 1;
ALTER TABLE `ban`
 DROP COLUMN `unbanned_ip`
, DROP COLUMN `a_ip`
, DROP COLUMN `ip`
, DROP COLUMN `serverip`
, CHANGE COLUMN `ipTEMP` `ip` INT(10) UNSIGNED NOT NULL
, CHANGE COLUMN `a_ipTEMP` `a_ip` INT(10) UNSIGNED NOT NULL
, CHANGE COLUMN `unbanned_ipTEMP` `unbanned_ip` INT(10) UNSIGNED NULL DEFAULT NULL;
COMMIT;

START TRANSACTION;
ALTER TABLE `connection_log`
 ADD COLUMN `server_ip` INT UNSIGNED NOT NULL AFTER `serverip`
, ADD COLUMN `server_port` SMALLINT UNSIGNED NOT NULL AFTER `server_ip`
, ADD COLUMN `ipTEMP` INT UNSIGNED NOT NULL AFTER `ip`;
SET SQL_SAFE_UPDATES = 0;
UPDATE `connection_log`
 SET `server_ip` = INET_ATON(SUBSTRING_INDEX(IF(`serverip` = '', '0', IF(SUBSTRING_INDEX(`serverip`, ':', 1) LIKE '%_._%', `serverip`, '0')), ':', 1))
, `server_port` = IF(`serverip` LIKE '%:_%', CAST(SUBSTRING_INDEX(`serverip`, ':', -1) AS UNSIGNED), '0')
, `ipTEMP` = INET_ATON(IF(`ip` LIKE '%_._%', `ip`, '0'));
SET SQL_SAFE_UPDATES = 1;
ALTER TABLE `connection_log`
 DROP COLUMN `ip`
, DROP COLUMN `serverip`
, CHANGE COLUMN `ipTEMP` `ip` INT(10) UNSIGNED NOT NULL;
COMMIT;

START TRANSACTION;
SET SQL_SAFE_UPDATES = 0;
UPDATE `death`
 SET `bruteloss` = LEAST(`bruteloss`, 65535)
, `brainloss` = LEAST(`brainloss`, 65535)
, `fireloss` = LEAST(`fireloss`, 65535)
, `oxyloss` = LEAST(`oxyloss`, 65535);
ALTER TABLE `death`
 CHANGE COLUMN `pod` `pod` VARCHAR(50) NOT NULL
, CHANGE COLUMN `coord` `coord` VARCHAR(32) NOT NULL
, CHANGE COLUMN `mapname` `mapname` VARCHAR(32) NOT NULL
, CHANGE COLUMN `job` `job` VARCHAR(32) NOT NULL
, CHANGE COLUMN `special` `special` VARCHAR(32) NULL DEFAULT NULL
, CHANGE COLUMN `name` `name` VARCHAR(96) NOT NULL
, CHANGE COLUMN `byondkey` `byondkey` VARCHAR(32) NOT NULL
, CHANGE COLUMN `laname` `laname` VARCHAR(96) NULL DEFAULT NULL
, CHANGE COLUMN `lakey` `lakey` VARCHAR(32) NULL DEFAULT NULL
, CHANGE COLUMN `gender` `gender` ENUM('neuter', 'male', 'female', 'plural') NOT NULL
, CHANGE COLUMN `bruteloss` `bruteloss` SMALLINT UNSIGNED NOT NULL
, CHANGE COLUMN `brainloss` `brainloss` SMALLINT UNSIGNED NOT NULL
, CHANGE COLUMN `fireloss` `fireloss` SMALLINT UNSIGNED NOT NULL
, CHANGE COLUMN `oxyloss` `oxyloss` SMALLINT UNSIGNED NOT NULL
, ADD COLUMN `server_ip` INT UNSIGNED NOT NULL AFTER `server`
, ADD COLUMN `server_port` SMALLINT UNSIGNED NOT NULL AFTER `server_ip`;
UPDATE `death`
 SET `server_ip` = INET_ATON(SUBSTRING_INDEX(IF(`server` = '', '0', IF(SUBSTRING_INDEX(`server`, ':', 1) LIKE '%_._%', `server`, '0')), ':', 1))
, `server_port` = IF(`server` LIKE '%:_%', CAST(SUBSTRING_INDEX(`server`, ':', -1) AS UNSIGNED), '0');
SET SQL_SAFE_UPDATES = 1;
ALTER TABLE `death`
 DROP COLUMN `server`;
COMMIT;

ALTER TABLE `library`
 CHANGE COLUMN `category` `category` ENUM('Any', 'Fiction', 'Non-Fiction', 'Adult', 'Reference', 'Religion') NOT NULL
, CHANGE COLUMN `ckey` `ckey` VARCHAR(32) NOT NULL DEFAULT 'LEGACY'
, CHANGE COLUMN `datetime` `datetime` DATETIME NOT NULL
, CHANGE COLUMN `deleted` `deleted` TINYINT(1) UNSIGNED NULL DEFAULT NULL;

ALTER TABLE `messages`
 CHANGE COLUMN `type` `type` ENUM('memo', 'message', 'message sent', 'note', 'watchlist entry') NOT NULL
, CHANGE COLUMN `text` `text` VARCHAR(2048) NOT NULL
, CHANGE COLUMN `secret` `secret` TINYINT(1) UNSIGNED NOT NULL;

START TRANSACTION;
ALTER TABLE `player`
 ADD COLUMN `ipTEMP` INT UNSIGNED NOT NULL AFTER `ip`;
SET SQL_SAFE_UPDATES = 0;
UPDATE `player`
 SET `ipTEMP` = INET_ATON(IF(`ip` LIKE '%_._%', `ip`, '0'));
SET SQL_SAFE_UPDATES = 1;
ALTER TABLE `player`
 DROP COLUMN `ip`
, CHANGE COLUMN `ipTEMP` `ip` INT(10) UNSIGNED NOT NULL;
COMMIT;

START TRANSACTION;
ALTER TABLE `poll_question`
 CHANGE COLUMN `polltype` `polltype` ENUM('OPTION', 'TEXT', 'NUMVAL', 'MULTICHOICE', 'IRV') NOT NULL
, CHANGE COLUMN `adminonly` `adminonly` TINYINT(1) UNSIGNED NOT NULL
, CHANGE COLUMN `createdby_ckey` `createdby_ckey` VARCHAR(32) NULL DEFAULT NULL
, CHANGE COLUMN `dontshow` `dontshow` TINYINT(1) UNSIGNED NOT NULL
, ADD COLUMN `createdby_ipTEMP` INT UNSIGNED NOT NULL AFTER `createdby_ip`
, DROP COLUMN `for_trialmin`;
SET SQL_SAFE_UPDATES = 0;
UPDATE `poll_question`
 SET `createdby_ipTEMP` = INET_ATON(IF(`createdby_ip` LIKE '%_._%', `createdby_ip`, '0'));
SET SQL_SAFE_UPDATES = 1;
ALTER TABLE `poll_question`
 DROP COLUMN `createdby_ip`
, CHANGE COLUMN `createdby_ipTEMP` `createdby_ip` INT(10) UNSIGNED NOT NULL;
COMMIT;

START TRANSACTION;
ALTER TABLE `poll_textreply`
 CHANGE COLUMN `replytext` `replytext` VARCHAR(2048) NOT NULL
, ADD COLUMN `ipTEMP` INT UNSIGNED NOT NULL AFTER `ip`;
SET SQL_SAFE_UPDATES = 0;
UPDATE `poll_textreply`
 SET `ipTEMP` = INET_ATON(IF(`ip` LIKE '%_._%', `ip`, '0'));
SET SQL_SAFE_UPDATES = 1;
ALTER TABLE `poll_textreply`
 DROP COLUMN `ip`
, CHANGE COLUMN `ipTEMP` `ip` INT(10) UNSIGNED NOT NULL;
COMMIT;

START TRANSACTION;
ALTER TABLE `poll_vote`
 CHANGE COLUMN `ckey` `ckey` VARCHAR(32) NOT NULL
, ADD COLUMN `ipTEMP` INT UNSIGNED NOT NULL AFTER `ip`;
SET SQL_SAFE_UPDATES = 0;
UPDATE `poll_vote`
 SET `ipTEMP` = INET_ATON(IF(`ip` LIKE '%_._%', `ip`, '0'));
SET SQL_SAFE_UPDATES = 1;
ALTER TABLE `poll_vote`
 DROP COLUMN `ip`
, CHANGE COLUMN `ipTEMP` `ip` INT(10) UNSIGNED NOT NULL;
COMMIT;

/*----------------------------------------------------

These queries are to be run after the above.
These indexes are designed with only the codebase queries in mind.
You may find it helpful to modify or create your own indexes if you utilise additional queries for other services.

----------------------------------------------------*/

ALTER TABLE `ban`
 ADD INDEX `idx_ban_checkban` (`ckey` ASC, `bantype` ASC, `expiration_time` ASC, `unbanned` ASC, `job` ASC)
, ADD INDEX `idx_ban_isbanned` (`ckey` ASC, `ip` ASC, `computerid` ASC, `bantype` ASC, `expiration_time` ASC, `unbanned` ASC)
, ADD INDEX `idx_ban_count` (`id` ASC, `a_ckey` ASC, `bantype` ASC, `expiration_time` ASC, `unbanned` ASC);

ALTER TABLE `ipintel`
 ADD INDEX `idx_ipintel` (`ip` ASC, `intel` ASC, `date` ASC);

ALTER TABLE `library`
 ADD INDEX `idx_lib_id_del` (`id` ASC, `deleted` ASC)
, ADD INDEX `idx_lib_del_title` (`deleted` ASC, `title` ASC)
, ADD INDEX `idx_lib_search` (`deleted` ASC, `author` ASC, `title` ASC, `category` ASC);

ALTER TABLE `messages`
 ADD INDEX `idx_msg_ckey_time` (`targetckey` ASC, `timestamp` ASC)
, ADD INDEX `idx_msg_type_ckeys_time` (`type` ASC, `targetckey` ASC, `adminckey` ASC, `timestamp` ASC)
, ADD INDEX `idx_msg_type_ckey_time_odr` (`type` ASC, `targetckey` ASC, `timestamp` ASC);

ALTER TABLE `player`
 ADD INDEX `idx_player_cid_ckey` (`computerid` ASC, `ckey` ASC)
, ADD INDEX `idx_player_ip_ckey` (`ip` ASC, `ckey` ASC);

ALTER TABLE `poll_option`
 ADD INDEX `idx_pop_pollid` (`pollid` ASC);

ALTER TABLE `poll_question`
 ADD INDEX `idx_pquest_question_time_ckey` (`question` ASC, `starttime` ASC, `endtime` ASC, `createdby_ckey` ASC, `createdby_ip` ASC)
, ADD INDEX `idx_pquest_time_admin` (`starttime` ASC, `endtime` ASC, `adminonly` ASC)
, ADD INDEX `idx_pquest_id_time_type_admin` (`id` ASC, `starttime` ASC, `endtime` ASC, `polltype` ASC, `adminonly` ASC);

ALTER TABLE `poll_vote`
 ADD INDEX `idx_pvote_pollid_ckey` (`pollid` ASC, `ckey` ASC)
, ADD INDEX `idx_pvote_optionid_ckey` (`optionid` ASC, `ckey` ASC);

ALTER TABLE `poll_textreply`
 ADD INDEX `idx_ptext_pollid_ckey` (`pollid` ASC, `ckey` ASC);
