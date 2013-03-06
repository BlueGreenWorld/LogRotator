#LogRotator#
LogRotator will free disk resources automatically for you, it resides as windows service and primarily does two things.
 
* Compress log, xls or any other files to increase space, they can be extracted with any zip archive handler.
* Delete files older than specific duration, based on last modified timestamp
 
##How to Configure##
It works on LogRotator.xml configuration file, pretty much all tags are self-explanatory, you will find details about each tag in the configuration file.
 
##Ideal Configuration##
Offset duration for compressing any log files should be more than 2 to 3 days so that it would be easier for developers to send their reports or logs without the need for decompression.
 
##Install/Uninstall##
[Download](https://github.com/farazmasoodkhan/LogRotator/downloads) latest release and extract archive into appropiate folder than simply run Install.bat file to install this service (as administrator), and run Uninstall.bat to uninstall from host machine. Service is set to run automatically. Remember service will run from the same folder from where you will run Install.bat, so extracts files in the appropriate folder first.
 
##Logs##
LogRotator also generates its own logs, therefore you will find two configurations for LogRotator service itself in the config file. Logs will be compress after two days and deleted after seven days. It is recommended to keep theses settings in the config.
 
##Requirement##
It requires .NET 4.0 Extended Profile to be installed on host machine.

##Warranty##
This service comes on as is bases, with no warranty to work flawlessly.

**Copyright © [FanaticLab](http://www.fanaticlab.com) 2013.**

