# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [6.0.0] - 2018-05-24
### Changed
 - Crosspoint SPlus shims reworked to new paradigm

## [5.0.1] - 2018-05-09
### Changed
 - Fixed subscription bug in Heartbeat

## [5.0.0] - 2018-04-25
### Changed
 - Fixed appearances of broadcasters in console
 - Message managers can now be registered without knowing message type

## [4.1.0] - 2018-04-23
### Added
 - Adding an explicit BroadcastData JSON converter
 - Additional console commands for handling the BroadcastManager

### Changed
 - Fixed ObjectDisposedException in Net Standard when stopping the AsyncUdpClient

## [4.0.0] - 2018-04-15
### Added
 - DirectMessageManager better supports sending messages from the server to specific clients

### Changed
 - Improved various log messages for SSH and serial ports
 