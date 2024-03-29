# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [17.1.0]
### Added
 - IPort now has DefaultDebuggingMode that can be set by devices
 - ISerialQueue now has properties to get CurrentCommand and Buffer

### Changed
 - AbstractPort implements IDefaultDebuggingMode
 - SerialQueue implements CurrentCommand and Buffer properties
 - Removed Obfuscation

## [17.0.0] - 2023-02-13
### Changed
 - IR Commands changed to use a int offset
 - MockIoPort can now set input status from the console

## [16.0.0] - 2022-12-24
### Removed
 - Moved XP3 UI Bindings to ICD.Connect.Themes.Crosspoints

## [15.3.0] - 2022-12-02
### Changed
 - Changed IR Pulse Component to fix deadlock issues and allow stopping processing the queue

## [15.2.2] - 2022-09-23
### Changed
 - Changed Crestron HTTP port to use a new client for each request

## [15.2.1] - 2022-08-04
### Changed
 - Removed Crestron HTTP url encoding hack, since they fixed it and the hack broke it

## [15.2.0] - 2022-07-13
### Changed
 - IcdUdpServer allows different AcceptAddresses to be used
 - IcdUdpSocketPool warns of multiple servers using the same port with different AcceptAddresses
 - XP3 and BroadcastManager use correct multicast AccpetAddress, fixes multicast discovery

## [15.1.1] - 2022-07-11
### Changed
 - Fixed preprocessors
 - Fixed package reference for NetStandard target

## [15.1.0] - 2022-07-01
### Added
 - eHardButton enum and eButtonAction (moved from ICD.Connect.Panels)
 
### Changed
 - Fixed null ref exception when debugging IR ports
 - Fixed bug disposing of AbstractPortServerDevices
 - Removed Heartbeat property from IConnectable
 - IcdUdpSocket now implements IConnectable
 - IcdUdpSocketPool now uses heartbeats to keep sockets connected
 - IcdTcpServer potential fixes for listening issues on stopping server
 - Added ComSpecProperties exteison to copy from ComSpec
 - Updated Crestron SDK to 2.18.96

## [15.0.0] - 2022-05-23
### Added
 - IrPortServerDevice - allowing control of Krang IR ports from other applications
 - WebQueue - PriorityQueue that regulates the sending of HTTP requests
 - Add HttpClient back into SimplSharp HttpPort
 - IcdUdpServer - Intended for use when a specific local port is needed to accept packets from other devices
 - Added request body support to HTTP/S GET requests
 
### Changed
 - SerialPortServerDevice inherits from AbstractPortServerDevice
 - IrPulseComponent - changed press/release to be actions passed into constructor
 - IrPulseComponent - fixed potential deadlocks, fix pulse time duration
 - WebPortResponse Success property split into GotResponse & IsSuccessCode properties
 - IcdUdpClient - changed client to use ephemeral local port instead of the report port
 - IcdUdpClient - no longer uses UDP socket pool
 - BroadcastManager - now uses IcdUdpServer instead of IcdUdpClient
 - Crosspoint Advertisement Manager - now uses IcdUdpServer instead of IcdUdpClient
 - Crosspoint Info Converter - updated to support ReadObject with an existing value
 - Performance improvements by no longer generating debug strings when debugging is disabled
 - Caching HTTP/S serial data
 - XP3 UI Bindings split to seperate Equipment and Control bindings
 - XP3 Sig Helper methods moved to extension class
 - Fixed null ref exception in IrPort debugging

## [14.2.0] - 2021-10-04
### Changed
 - Fixed IR Pulse Component to pulse for the correct pulse time, instead of the total duration
 - Removed SafeCriticalSection.TryEnter from HttpPort.Standard, FeedbackDebounce, Heartbeat, and AbstractSerialBuffer
 - IcdTcpClient, IcdSecureTcpClient, AbstractServer - Dispose of ThreadedWorkerQueues
 - Added debugging to MockIrPort

## [14.1.0] - 2021-08-18
### Added
 - Added SerialPortAdapter for controlling NetStandard serial ports
 - Added NamedPipeClient and NamedPipeServer for communicating between processes
 - Added XP3 service class

### Changed
 - Fixed a bug where PFX auto-generation would fail if the output directory did not exist yet
 - Fixed a mistake preventing configuring MQTT proxy port via console
 - Fixed a bug where IR pulses would never release
 - Fixed a bug where servers would sometimes return a null console name, breaking the console
 - Improved support for device control via UDP

## [14.0.0] - 2021-05-14
### Added
 - Added overloads to RPC server for calling a method on all connected clients
 - Support configured Crestron IR driver files & custom .csv files
 - Add new console node for URI Properties

### Changed
 - Abstracted IR pulsing & timing logic from Crestron IrPortAdapter to IrPortPulseComponent
 - MockMqttClient now "publishes" messages to an internal class. Added console command for viewing messages.
 - Generated certs have a longer expiration window
 - Fixed a bug where certificate validation was not using UTC
 - Using existing generated certificates if still valid instead of generating a new one
 - Obfuscate URI passwords when printing them to console
 - Fixed bugs where TCP server would sometimes fail to lookup a client by ID
 - Fixed exception related to generating TCP certs with a negative hash

## [13.4.1] - 2022-02-08
### Changed
 - Fixed a bug where default network servers would have an empty name and become unavailable in the console
 - Fixed BouncyCastle references and added reference to SimplSharpCryptographyInterface

## [13.4.0] - 2021-01-14
### Added
 - Added PATCH requests to IWebPort and Implemented in HttpPort
 - Added PUT requests to IWebPort and Implemented in HttpPort
 - Added secure TCP server and client
 - Added X509Utils for automatic certificate generation
 - Added ConfigurePort option to ClientSerialRpcController
 - Added EchoComparer function to Serial Queue to skip echoed responses

### Changed
 - Dispatch SOAP now uses the URI directly on the port when creating HTTPRequestMessages
 - TCP Clients/Servers now use synchronous send commands to ensure messages are sent in-order, with a ThreadedWorkerQueue
 - Fixed NetStandard TCP Servers to use configured BufferSize correctly
 - Changed various property XML parsing routines to parse an empty string as null, so overrides work correctly
 - Changed TCPClient and SecureTCPClient settings to not use the default port in the network properties
 - Changed Crestron TCPServer and SecureTCPServer to use "WaitForConnectionAlways" - resolves some multi-client issues
 - Added local port number to TCPClient status
 - Added individual client console access to TCPClientPool
 - Added configurable MaxClients to IcdConsoleServer
 - Changed ICDConsoleServer to start on StartSettings

## [13.3.3] - 2020-10-12
### Changed
 - Fixed a deadlock in AbstractSerialBuffer on Clear(), and made it so clear would actually clear the buffer before emptying it

## [13.3.2] - 2020-09-30
### Changed
 - Fixed a bug where the MQTT client was not closing the connection on disconnect, causing LWT issues

## [13.3.1] - 2020-09-24
### Changed
 - Fixed a bug where default port activities were not being initialized

## [13.3.0] - 2020-09-11
### Changed
 - MQTT messages are buffered in the ICD client until the client connects

## [13.2.2] - 2020-08-25
### Changed
 - Fixed a bug that was preventing SSH config generation
 - Fixed an MQTT bug that was preventing SSL from working on net standard

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

## [12.2.0] - 2021-03-08
### Changed
 - ISerialQueue now supports DisconnectClearTime, to allow a port to reconnect before clearing the queue
 - ISerialQueue now raises OnSendFailed event when a port fails to send data.

## [12.1.0] - 2020-10-06
### Changed
 - Implemented StartSettings for SerialPortServer to start listening
 - Changes to ClientSerialRpcController to support using StartSettings in implementors
 - Fixed issue with Heartbeat where a null reference exception is thrown if the instance is cleared while connect is blocking
 - Fixed issue with Heartbeat where if StopMonitoring is called while connect is blocking it will continue to try and connect

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
 