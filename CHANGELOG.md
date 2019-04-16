# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed
 - SSH converts "localhost" address to "127.0.0.1" due to host resolution failing

## [8.1.0] - 2019-01-29
### Added
 - Added program number to port debug lines
 - Added SystemId property to BroadcastManager
 - SerialPortServerDevice to make a ISerialPort avaliable over a TCP Server

### Changed
 - Performance improvements for JsonSerialBuffer
 - XP3 Changes to work with new SPlusShim paradigm
 - XP3 NullRef fixes
 - SigCache JSON optimizations to better handle large amounts of data

## [8.0.0] - 2019-01-10
### Added
 - Added interfaces for port configuration
 - Added port configuration features to existing ports

### Changed
 - Numerous ComSpec refactorings
 - Improvements to WebPort URI building

### Removed
 - Removed ComPortPlus and other unused code

## [7.3.1] - 2019-04-05
### Changed
 - Better HttpPort online state tracking when a request dispatch fails
 - Better thread safety for port debugging

## [7.3.0] - 2019-01-02
### Changed
 - AbstractSerialQueue clears IsCommandInProgress when clearing queue.
 - Failing more gracefully when trying to start multiple TCP servers with the same address in Net Standard
 - BroadcastManager no longer attempts to broadcast while the UDP client is disconnected

## [7.2.1] - 2018-11-20
### Changed
 - Heartbeat forces a reconnection after an interval, allowing devices to deinitialize

## [7.2.0] - 2018-10-30
### Added
 - Colour-coded port debugging
 - IO port configuration can be set via settings
 - SerialQueue exposes EnqueuePriority method with comparer

## [7.1.0] - 2018-10-18
### Changed
 - Better URI building
 - TCP threading improvements for Net Standard
 - Logging improvements

## [7.0.1] - 2018-09-25
### Changed
 - ConnectionStateManager provides more accurate feedback
 - Logging serial port connection status

## [7.0.0] - 2018-09-14
### Added
 - Added trust mode to serial queues
 - Added transmission event to serial queues

### Changed
 - Various performance improvements
 - TCP fixes for Net Standard

## [6.4.0] - 2018-07-19
### Added
 - Added console command to mock incoming data from serial ports
 - ConnectionStateManager exposes wrapped port
 
### Changed
 - Logging relay open/close state changes
 - Fixed bug where generic types would appear incorrectly in the console
 - Fixed potential null ref in ConnectionStateManager

## [6.3.0] - 2018-07-02
### Added
 - Added fallback authentication methods for SSH
 
### Changed
 - Added cancellation tokens to AsyncTcpClient to fix disconnect issues
 - Small fixes for SSH and TCP Server exceptions
 - Fixed ObjectDisposedException in Heartbeat

## [6.2.0] - 2018-06-19
### Added
 - SerialPort console support for sending escape codes (crlf, hex, etc)

### Changed
 - Fixing Heartbeat logging order

## [6.1.0] - 2018-06-04
### Added
 - Added ConnectionStateManager for standardized serial connection management between drivers

### Changed
 - RPC uses ConnectionStateManager for maintaining connection with remote endpoints
 - Fixed bug where a disconnected UDP client would throw an exception on network interface linkdown

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
 