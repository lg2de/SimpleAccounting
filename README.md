# SimpleAccounting - Simple Accounting Software

[![Build status](https://ci.appveyor.com/api/projects/status/gdw9q7ves4fuu9t4/branch/master?svg=true)](https://ci.appveyor.com/project/lg2de/simpleaccounting/branch/master)

I started `SimpleAccounting` around 2005, only for my personal use to manage accounts and balances of a small club.
In 2019, I met a colleague from another small club who was looking for a simple solution to get an overview of the balances of several logical and real accounts.

So I decided to rework my solution to make it usable for others and open source.

## Naming

The name says it all. The software is as simple as possible. 

It probably does not follow all rules and laws of double-entry accounting, e.g. the software does not protect accounting entries from changes.
The data is stored in readable XML format and can be changed manually.

The software focuses on journal and balance sheet reporting.

## Features

* Data management for double-entry accounting, stored as XML (see [example](./samples/sample.bxml)) and defined using [XSD](./docs/AccountingData.xsd)
* User interface for the management of accounts, the creation of entries, the listing of account journals
* Import of booking entries from CSV files incl. semi-automatic assignment of offsetting accounts
* Printable reports for full journal, account journal, accounts and balances, and annual financial statements

Some screenshots for first impression:

|Main view|Totals and balances report|
|-|-|
|<img src="./samples/MainView.png" alt="Main view" width="250" />|<img src="./samples/TotalsAndBalancesReport.png" alt="Totals and balances report" width="250" />|

## Contribution

Contributions are welcome!

Small changes can be made immediately via Pull Request. I will try to check and integrate it as soon as possible.
Medium and large changes should be discussed first. Please open a new issue or participate in existing discussions.

## TODOs

When I started the project in 2005, I saw no relevance for unit testing.
So far the test coverage is pretty bad:

[![codecov](https://codecov.io/gh/lg2de/SimpleAccounting/branch/master/graph/badge.svg)](https://codecov.io/gh/lg2de/SimpleAccounting)

With the decision this year to publish it, I started to switch to Test Driven Development.
I hope to increase coverage step by step. Maybe you want to help by creating component tests?

I am German and have no experience with English names for the financial world.
So I'm pretty sure that some of the words I use are confusing for native speakers.
Maybe you want to help with the better naming?

Also, the user interface is completely in German.
Maybe you want to help with the introduction of a localized user interface?

## Roadmap

Currently I'm working on completing the software into version 2.0.0, the first public release.
The software available in the repository is intended as contiguous alpha version:
No guarantee that a project can be loaded without manual modification.
The file format will be fixed only with the first beta release.

I hope to release the first beta in december 2019.
I plan to release the version 2.0.0 in january 2020.

## License

I do not like closed source reuse of my software.
This is why I decided to license the software under GPLV3.
