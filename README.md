# Introduction

This is a collection of various utilities and abstractions I use in .NET projects.

## Audit
* Store and query activities.

## Audit.LinqToPg
* Data.LinqToDB Audit provider for Postgres.

## Byte
* Exension methods to convert byte arrays to hex strings and back. 
* BCD encoder/decoder.

## CircularBuffer
* Generic circular buffer.

## Data
* Database abstractions.

## Data.LinqToDB
* LinqToDB provider for zcfux.Data.

## Data.Postgres
* Npgsql provider for zcfux.Data.

## DI
* Dependency injection wrapper.

## Filter
* Exensions methods to build abstract syntax trees for querying data.
* Abstract syntax trees can be converted to LINQ expressions.

## JobRunner
* Run and schedule (periodic) tasks.
* Customizable job queue (library comes with an in-memory queue).

## JobRunner.Data
* JobRunner database abstractions.

## JobRunner.Data.LinqToDB
* Persistent job queue using LinqToDB.

## KeyValueStore
* Key-value store abstractions and in-memory implementation.
 
## KeyValueStore.Persistent
* A persistent key-value store implementation for storing large blobs.
* Small blobs are stored in a SQLite database.
* Large blobs are stored as files.
* Blobs are addressable by their content to ensure integrity and save disk space.
 
## Logging
* Logging abstractions.

## Logging.NLog
* NLog provider for zcfux.Logging.

## Mail
* Mail storage abstractions.

## Mail.LinqToPg
* Data.LinqToDB Mail provider for Postgres.

## Pool
* Generic and customizable resource pool.

## PubSub
* Publish & subsribe building block.

## Replication
* Replication abstractions.

## Replication.CouchDb
* CouchDB provider for zcfux.Replication.

## Security
* Password generator.
* Message signing.
* Rate limiting.

## Session
* Session management with customizable storage.

## SqlMapper
* Tiny object mapper for ADO.NET.

## Tracking
* Track state changes to objects.

## User
* User, group & permission management.

## User.LinqToDB
* Data.LinqToDB User provider.