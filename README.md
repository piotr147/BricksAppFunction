# Bricks App Functions

Project used for monitoring prices of Lego sets. User can subscribe set and gets notified when lowest price changes.

## Technologies
* .Net Core
* Azure Functions
* Sql Server
* SendGrid

## How it works

Every X minutes (for example every 10 minutes) Azure time trigger function processes every Lego set that is subscribed by at least one user. It checks [Promoklocki](https://promoklocki.pl/) website that gathers and compares sets' prizes. If lowest price changed since last visit, database is updated with new information. Next, results are collected for every user and are sent using SendGrid.

Azure funcitons Http triggers are used for creating and managing subscriptions.

## Example mail with price updates

![mail example](https://github.com/piotr147/BricksAppFunction/blob/master/Images/mail.png)
