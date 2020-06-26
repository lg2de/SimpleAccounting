# SimpleAccounting - Simple Accounting Software

![Build status](https://github.com/lg2de/SimpleAccounting/workflows/continous%20integration/badge.svg?branch=master)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=lg2de_SimpleAccounting&metric=coverage)](https://sonarcloud.io/dashboard?id=lg2de_SimpleAccounting)

[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=lg2de_SimpleAccounting&metric=alert_status)](https://sonarcloud.io/dashboard?id=lg2de_SimpleAccounting)
[![Maintainability Rating](https://sonarcloud.io/api/project_badges/measure?project=lg2de_SimpleAccounting&metric=sqale_rating)](https://sonarcloud.io/dashboard?id=lg2de_SimpleAccounting)
[![Reliability Rating](https://sonarcloud.io/api/project_badges/measure?project=lg2de_SimpleAccounting&metric=reliability_rating)](https://sonarcloud.io/dashboard?id=lg2de_SimpleAccounting)
[![Security Rating](https://sonarcloud.io/api/project_badges/measure?project=lg2de_SimpleAccounting&metric=security_rating)](https://sonarcloud.io/dashboard?id=lg2de_SimpleAccounting)

**SimpleAccounting** is a simple software for accounting.
It supports double-entry accounting including different reports.
Bookings can be imported from text files.

I started `SimpleAccounting` around 2005, only for my personal use to manage accounts and balances of a small club.
In 2019, I met a colleague from another small club who was looking for a simple solution to get an overview of the balances of several logical and real accounts.

So I decided to rework my solution to make it usable for others and open source.

## Naming

The name says it all. The software is as simple as possible. 

It probably does not follow all rules and laws of double-entry accounting, e.g. the software does not protect accounting entries from changes.
The data is stored in readable XML format and can be changed manually.

The software focuses on journal and balance sheet reporting.

## Features

* Data management for double-entry accounting, stored as XML (see [example](./samples/sample.acml)) and defined using [XSD](./docs/AccountingData.xsd)
* User interface for the management of accounts, the creation of entries, the listing of account journals
* Support for split booking entries, either on credit or debit side
* Import of booking entries from CSV files incl. semi-automatic assignment of offsetting accounts
* Printable reports for full journal, account journal, accounts and balances, and annual financial statements

Some screenshots for first impression:

|Main view|Split booking|Totals and balances report|
|-|-|-|
|<img src="./samples/MainView.png" alt="Main view" width="250" />|<img src="./samples/SplitBooking.png" alt="Split booking" width="250" />|<img src="./samples/TotalsAndBalancesReport.png" alt="Totals and balances report" width="250" />|

## Getting started

The application is based on [.NET Core 3](https://dotnet.microsoft.com/download/dotnet-core/3.1).
Please download and install the Runtime in version [3.1](https://dotnet.microsoft.com/download/dotnet-core/thank-you/runtime-desktop-3.1.5-windows-x64-installer) or above.

Download the `SimpleAccounting` package from the [release page](https://github.com/lg2de/SimpleAccounting/releases).
Extract the ZIP archive into your preferred destination directory.

Start `SimpleAccounting.exe`.

## Contribution

Contributions are welcome!

Small changes can be made immediately via Pull Request. I will try to check and integrate it as soon as possible.
Medium and large changes should be discussed first. Please open a new issue or participate in existing discussions.

## TODOs

I am German and have no experience with English names for the financial world.
So I'm pretty sure that some of the words I use are confusing for native speakers.
Maybe you want to help with the better naming?

Also, the user interface is completely in German.
Maybe you want to help with the introduction of a localized user interface?

## Roadmap

The public releases are [available](https://github.com/lg2de/SimpleAccounting/releases).
Please download, test and send feedback.

The roadmap is defined by issues and milestones.

## License

I do not like closed source reuse of my software.
This is why I decided to license the software under GPLV3.
