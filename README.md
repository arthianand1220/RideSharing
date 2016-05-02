Name: P120X
Description: Ride sharing application which merges the trips based on the location. Single source is taken
and all the requests within specific timeframe are pulled out. Then these requests are analyzed and merged based 
on few parameters.

===========================================
Developers: 
Murali Krishna Valluri
Dilip Vakkalanka
Spoorthi Pendyala
Arthi Anand

===========================================
Requirements:
3 PCs to setup the entire application
1st PC hosts the ride sharing application
2nd PC hosts the OSRM-Backend with walking profile.
3rd PC hosts the OSRM-Backend with driving profile.

===========================================
Steps to setup the development server:
- Download and setup OSRM-Backend one on each PC with walking and driving profile. 
  You can follow the tutorial on their wiki page. (https://github.com/Project-OSRM/osrm-backend/wiki/Building-OSRM)
- Now install SQL Server and run the db scripts present in the sql folder.
- Now open the sln file in Visual Studio 2015 and build the solution.
- Once the build is successful, launch the application in the browser.
- Select the dataset to import, specify maximum waiting and walking time and hit import.
- Once the file is imported, a success message would be displayed.
- To run the simulations, open TripProcessorTests in RideSharing project. To test the project, change the
  walking and driving URLs.
- Once that is done right click on the test and hit RunTests.
- The results would be saved in database.
