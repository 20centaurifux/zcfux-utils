# Introduction

This is a collection of various utilities and abstractions I use in .NET projects.

## Audit
* Store and query activities.

## Audit.LinqToDB
* Data.LinqToDB Audit provider.

## Audit.LinqToPg
* Data.LinqToDB Audit provider optimized for Postgres.

## Byte
* Exension methods to convert byte arrays to hex strings and back. 
* BCD encoder/decoder.

## CircularBuffer
* Generic circular buffer.

## CredentialStore
* Credential store abstractions and in-memory implementation.

## CredentialStore.SQLite
* SQLite credential store implementation with AES encryption.

## CredentialStore.Vault
* HashiCorp Vault credential store implementation.

## Data
* Database abstractions.

## Data.LinqToDB
* LinqToDB zcfux.Data provider.

## Data.Postgres
* Npgsql zcfux.Data provider.

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
* NLog for zcfux.Logging provider.

## Mail
* Mail storage & transfer abstractions.

## Mail.LinqToPg
* Data.LinqToDB Mail provider for Postgres.

## Mail.MailKit
* SMTP transfer agent using MailKit.

## Pool
* Generic and customizable resource pool.

## PubSub
* Publish & subsribe building block.

## Replication
* Replication abstractions.

## Replication.CouchDb
* CouchDB zcfux.Replication provider.

## Security
* Password generator.
* Message signing.
* Rate limiting.

## Session
* Session management with customizable storage.

## SqlMapper
* Tiny object mapper for ADO.NET.

## Telemetry
* Telemetry abstractions.

## Telemetry.MQTT
* MQTT zcfux.Telemetry provider.

## Tracking
* Track state changes to objects.

## Translation
* Translation abstractions.

## Translation.LinqToDB
* Data.LinqToDB Translation provider.

## User
* User, group & permission management.

## User.LinqToDB
* Data.LinqToDB User provider.