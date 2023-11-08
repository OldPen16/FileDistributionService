Pre-requisites:
1. Installed .net core of latest version. This project is build in .net core 6.0
2. Installed database, PostgresSQl, on your system

Steps to run:
1. Download the project folder and copy as your desired location.
2. Open the project in the Visual Studio software
3. Change folder and file paths in the appsettings.json as per location on your system. Also, change the host, userid and password in the default connection string in appsettings.json file
4. To setup database, run migration commands or manually restore given filedistributionservice.backup file
5. Build and the project is ready to run
6. You can use POSTMAN to post request and get response from this API