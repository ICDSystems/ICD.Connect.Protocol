# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [13.2.1] - 2020-08-13
### Changed
 - Catching MQTT exceptions on disconnect
 - Updated M2MQTT library

## [13.2.0] - 2020-07-29
### Added
 - Added proxy support to IcdMqttClient
 - Added console commands for enabling/disabling crestron HTTP/S verbose mode
 - Added helpers for DeployAV configuration of SSH private key

## [13.1.0] - 2020-07-14
### Added
 - Added method to set the HTTP Authorization header if there is a username in the URI

### Changed
 - Reverted async SerialBuffer change due to deadlocks

## [13.0.1] - 2020-06-24
### Changed
 - Fixed IcdMqttClient exception when attempting to subscribe to an empty array of topics
 - Fixed IcdMqttClient exception when attempting to load a certificate from a bad path

## [13.0.0] - 2020-06-19
### Added
 - Added MQTT client port
 - Added WebSocket server
 - Added ResponseUrl to WebPortResponse
 - XP3 equipment manager is exposed in the console

### Changed
 - Using new logging context
 - Rewrote RPC JSON serialization
 - Fixed issues with port debug formatting
 - ConnectionStateManager supports any connectable port, not just serial ports
 - Simplified TCP server implementations
 - Fixed a bug related to casting when enqueuing commands against a serial queue for de-duplication
 - Serial buffers are asynchronous, provides a stability improvement for multi-system
 - Improvements to XP3 logging and validation
 - Fixed an XP3 bug related to null crosspoints
 - Automatically starting the console server device
 - TCP server defaults to 64 max clients

## [12.0.1] - 2020-03-20
### Changed
 - Crestron TCPServer now spawns a new thread to handle SocketStatusChanged event - mitigation for TCPServer bugs
 - Crestron TCPServer - Fixed issue where connected but not listening would be interpreted as listening
 - Crestron TCPServer - Better handling and recovery when hitting max number of connections - will now allow additional connections when no longer at max

## [12.0.0] - 2020-03-20
### Added
 - Added WebPortResponse to help IWebPort return byte arrays and headers instad of just strings.
 - MockIoPort sets configuration and digital out
 - Added PulseOpen and PulseClose methods for toggling relays
 - Added ConsoleServerDevice to host the ICD console as a TCP server
 - Added NetworkPro project
 - Added IcdMqttClient port
 
### Changed
 - Fixed CongureProxySettings allowing to make more than one request.
 - Fixed web requests to use new web port response.
 - Using UTC to track durations

## [11.0.0] - 2019-11-19
### Added
 - Added extensions for setting relay open/closed state
 - Added WebProxySettings and WebProxyProperties
 - Added web proxy features to existing web ports
 - SSHPort supports configurable private keys with pass-phrases

### Changed
 - For HTTP ports the URI builder will not try to use the ports URI if it is null when getting the request URL.
 - Greatly simplified IcdTcpServer, potential fixes for deadlocks
 - DelimiterSerialBuffer now optionally returns empty responses, defaults to off

## [10.0.0] - 2019-09-16
### Changed
 - AsyncTcpClient and AsyncTcpServer renamed to IcdTcpClient and IcdTcpServer
 - CSM no longer tries to reconnect on send - fixes synchronous UI problems

## [9.0.0] - 2019-08-15
### Added
 - XP3 NonCachingEquipmentCrosspoint

### Changed
 - XP3 ControlCrosspointManager AutoReconnect behaviour tries to reconnect beyond the initial disconnect
 - ConnectionStateManager clarifies which port is not connected on send

## [8.4.0] - 2019-08-15
### Changed
 - The ConnectionStateManager will try to connect to the port if it is not already connected before it sends data.
 - Added a JWT generator.
 - Fixed HTTP header compatibility in .Net Standard
 - Added table headers to AsyncTcpServer print clients console command

## [8.3.0] - 2019-07-03
### Added
 - Added HttpUtils for generating URL encoded content bodies
 - Added overloads for POST methods for accepting headers

### Changed
 - Crestron HTTP/S timeout increased to 60 seconds

## [8.2.5] - 2019-06-21
### Changed
 - Fixed a bug where configured URI queries would gain additional '?' prefixes

## [8.2.4] - 2019-06-06
### Added
 - Added better HTTPRequestException handling

### Changed
 - Fixed encoding issues with username and passwords
 - Fixed Invalid Soap Content Type
 - Fixed case where HTTP/S port would not go offline if a SOAP request fails to dispatch

## [8.2.3] - 2019-06-04
### Changed
 - Doubly-escaping HTTP/S paths for Crestron due to path encodings being stripped

## [8.2.2] - 2019-05-31
### Changed
 - Fixed a bug in Net Standard http port where it was not properly decoding extended ascii results
 - Changed HttpClient Timeout from 2 seconds to 5 seconds

## [8.2.1] - 2019-05-02
### Changed
 - AsyncTcpServer on .NET Standard now properly removes disconnected clients when reading 0 byte packets
 - Fixed mistake that was preventing URI paths from being applied to web ports
 - Failing more gracefully when handling invalid URIs
 - Treating empty URI XML elements as null

## [8.2.0] - 2019-04-16
### Added
 - Broadcasts contain a session id
 - Adding BufferSize property to AsyncTcpServer, setting large default
 - Exposing features for starting/stopping ConnectionStateManager heartbeat

### Changed
 - Better logging for malformed direct messages
 - Fixed SigCache issue for existing keys with new values
 - AsyncTcpServer has a default port
 - SSH converts "localhost" address to "127.0.0.1" due to host resolution failing

### Removed
 - Removing client disconnect hooks from message handlers, not relevant to consumers

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

## [7.7.1] - 2020-02-18
### Changed
 - Fixed issues with rate limited serial queues
 - Additional thread safety with serial queues

## [7.7.0] - 2020-02-14
### Added
 - Serial queue allows configuration of de-duplication behaviour

### Changed
 - Rate limiting features moved into Serial Queue
 - Thread safety improvements for serial queue

## [7.6.0] - 2019-11-18
### Added
 - Added hooks to ClientSerialRpcController to access ConnectionStateManager states

## [7.5.2] - 2019-10-29
### Added
 - Added a ToString() override for SerialData for clearer debugging

## [7.5.1] - 2019-06-18
### Changed
 - Failing gracefully when a JSON debug logging fails to parse cleanly
 - Thread safety improvements for Net Standard TCP server
 - Log improvements for Net Standard TCP server

## [7.5.0] - 2019-05-30
### Changed
 - Significant improvements to AsyncTcpServer recovery after network outages

## [7.4.0] - 2019-05-16
### Removed
 - Removed unused eSigIoMask enum 

## [7.3.2] - 2019-05-16
### Changed
 - ConnectionStateManager fails more gracefully when internal port is null
 - ConnectionStateManager does a better job of trapping errant feedback from the internal port

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
 