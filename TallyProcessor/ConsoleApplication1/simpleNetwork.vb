Imports System.Threading
Imports System.Net

''' <summary>
''' This simpleNetwork device class is meant as a working example of how to implement your own devices to TallyProcessor.
''' It is commented fairly pedantic, so you should have no problems understanding whats going on. I tried to use easy code that just demonstrates
''' the way the TallyProcessor core works and how it can be extended.
''' 
''' So, short explaination what this class does:
''' 
''' Propose:
''' --------
''' The simpleNetwork device works as input and output for tallyinformation provided via network.
''' The protocol beeing used is very simple. Each state change is discribed as a xml string similar
''' to the definition at the config file.
''' For outputs it's
''' <channel nr="Number" name="Name">true|false</channel>
''' and for inputs
''' <channel nr="Number">true|false</channel> or
''' <channel name="Name">true|false</channel>
''' Messages for the user are within
''' <mesg>MESSAGE</mesg>
''' tags.
''' 
''' Clients can connect to this device all the time. 
''' If a client is connected, it could send changeState messages to the device. There is no need to register for a special input.
''' So every client is able to change the same inputs.
''' And every connected client will recive all outputs hasChanged messages.
''' 
''' 
''' How it works:
''' -------------
''' The work is divided into 3 parts. One for the input changes, one for the output and one for the listening of incoming client connections.
''' The output is done directyl in the simpleNetwork class, but the input and connection listening will be
''' delegated to it's own working thread and class.
''' This is necessary because waiting for incoming messages and connections is done in a loop. 
''' This would make the whole TallyProcessor core waiting for us because it calls 
''' the init() in it's main thread.
''' The output changes are events triggered to us by other devices (it may be us as self, but then it will be the separated input thread),
''' so there is no waiting for the core thread.
''' That for, we create a own worker class, just waiting for incoming messages and delegating the needed state changesas well as one 
''' for incoming client connections.
''' 
''' To get a general understanding of Tallyprocessor please see the readme.txt and developer.txt in your app path.
''' 
''' 
''' Test it:
''' --------
''' To test the simpleNetwork you can use any telnet client or even raw clients like putty (which is what I'm using).
''' Just connect to the ip and port specified in the config file after TallyProcessor has been started.
''' You will see a message containing all configured output channels.
''' Type in new messages as discribed above and hit enter. The states should now change according to the config.
''' Your welcome to write your own littel Client as well and may share it with us by sending it to me (address see developer.txt).
''' 
''' </summary>
''' <remarks></remarks>
Public Class simpleNetwork
    Inherits device
    Implements IDevice

    ' is true if init() was called and ended successfully
    Private initState As Boolean

    ' The list of all connected clients
    Private clients As List(Of Sockets.Socket)
    'The socket we're listening to
    Private socket As Sockets.Socket
    Private port As Integer
    Private ip As Net.IPAddress

    'Monitor objects and threads
    Private connectionMonitor As connection_Monitor
    Private inputMonitor As simpleNetwork_Monitor
    Private inputThread As Thread
    Private connectionThread As Thread

    ''' <summary>
    ''' Creates a new Instance of simpleNetwork, a device class to send and recive tallyinformartion over network.
    ''' </summary>
    ''' <param name="name">the instance name</param>
    ''' <param name="config">a xml string defining needed configuration data</param>
    Public Sub New(ByVal name As String, ByVal config As String)
        MyBase.New(name, config)
        clients = New List(Of Sockets.Socket)
    End Sub

    ''' <summary>
    ''' Initializes the simpleNetwork device according to the configuration provided.
    ''' Make sure to add all inputs and outputs bevor calling init().
    ''' Normaly, this is called by the main process, so don't worry.
    ''' </summary>
    ''' <returns>
    ''' true - if and only if the simpleNetwork device is successfully initialized
    ''' false - otherwise.
    ''' </returns>
    Public Overrides Function init() As Boolean
        '' In this function, we will do everything needed to start simpleNetworks functionality.
        '' Once procceded and successfully ended, simpleNetwork should work with no need of interaction
        '' from the main thread nor from a user.

        '' Config:
        ''---------
        ' We have to open a socket to listen for incomming connections
        ' and there for we need to get the configuration setting ip and port first.
        config(getConfig)

        '' Network:
        ''----------
        ' Now set the socket as configured and let it listen. The buffer for incoming connections is 50
        logger.debug(getName() & ": Configuring network...")
        socket = New Sockets.Socket(Sockets.AddressFamily.InterNetwork, Sockets.SocketType.Stream, Sockets.ProtocolType.Tcp)
        socket.Bind(New IPEndPoint(ip, port))
        socket.Listen(50)
        socket.DontFragment = True
        ' Lets start the ConnectionMonitor to accept incoming connections
        clients = New List(Of Sockets.Socket)
        connectionMonitor = New connection_Monitor(socket, clients, Me)
        connectionThread = New Thread(AddressOf connectionMonitor.listen)
        connectionThread.Start()
        logger.log(getName() & ": Successfully startet listening for incoming connections at " & ip.ToString & ":" & port)

        '' Inputlistening:
        ''-----------------
        ' Last but not least, we have to start the input listening
        logger.debug(getName() & ": Configuring inputMonitor...")
        inputMonitor = New simpleNetwork_Monitor(clients, Me)
        inputThread = New Thread(AddressOf inputMonitor.monitor)
        inputThread.Start()
        logger.log(getName() & ": Successfully startet input monitoring.")

        ' Thats it, nothing left to do
        initState = True
        logger.debug(getName() & ": Device '" & getName() & "' is initialized.")
        Return initState
    End Function

    ''' <summary>
    ''' Close all resourcces used by this device. 
    ''' In case of simpleNetwork, it will close the socket and listening to new connections as well as monitoring input changes. 
    ''' </summary>
    ''' <remarks></remarks>
    Public Overloads Sub close()
        ' Stop listening for new clients
        connectionThread.Abort()

        'Stop monitoring inputs
        inputThread.Abort()

        'Disconnect all clients
        For Each client In clients
            client.Disconnect(True)
        Next
        clients.Clear()

        'Close socket
        socket.Disconnect(True)
        initState = False
    End Sub

    ''' <summary>
    ''' Returns whether or not, the simpleNetwork device is successfully initialized.
    ''' </summary>
    ''' <returns>
    ''' true - if and only if the simpleNetwork device is successfully initialized
    ''' false - otherwise.
    ''' </returns>
    Public Overrides Function isInitialized() As Boolean
        Return initState
    End Function

    ''' <summary>
    ''' Takes change informations of outputs.
    ''' </summary>
    ''' <param name="output">The output that changed</param>
    ''' <remarks></remarks>
    Public Overrides Sub hasChanged(ByRef output As output)
        ' Inform every connected client that the output changed
        ' It might be, that there is no actual change, but one input changed
        ' If you like, you could add a check to get less network io, but to
        ' keep it simple, we'll just send the message
        Dim changeMessage As Byte() = getMessage(output)
        Dim clientsToRemove As New List(Of Sockets.Socket)
        For Each client In clients
            Try
                client.Send(changeMessage, Sockets.SocketFlags.None)
            Catch e As Sockets.SocketException When e.SocketErrorCode = Sockets.SocketError.ConnectionAborted
                'It seems as the conncetion to the client is lost
                Dim info As IPEndPoint = client.RemoteEndPoint
                logger.debug(getName() & ": Client " & info.Address.ToString & ":" & info.Port.ToString & " is not connceted anymore. Removing it from list.")
                clientsToRemove.Add(client)
            End Try
        Next
        ' Remove disconnected clients
        For Each client In clientsToRemove
            clients.Remove(client)
        Next
    End Sub

    ''' <summary>
    ''' Computes a valid message of the given output as ByteArray to send via network.
    ''' </summary>
    ''' <param name="channel"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Friend Function getMessage(ByRef channel As ioChannel) As Byte()
        If Not IsNothing(channel) Then
            Dim messageString = "<channel nr='" & channel.getChannel.ToString & "' name='" & channel.getName & "'>" & channel.getState.ToString & "</channel>" & vbCrLf
            Return System.Text.ASCIIEncoding.ASCII.GetBytes(messageString)
        Else : Return System.Text.ASCIIEncoding.ASCII.GetBytes("")
        End If
    End Function

    ''' <summary>
    ''' In here we will parse the xml config for all paramters of interest
    ''' </summary>
    ''' <remarks></remarks>
    Private Sub config(ByVal config As String)
        ' We're paring the XML String for parameters of interest
        ' which are <ip> and <port>
        Dim configDoc As New MSXML2.DOMDocument
        configDoc.loadXML(config)

        ' Are there anything we could parse?
        If configDoc.hasChildNodes Then
            ' check for the ip
            If Not IsNothing(configDoc.firstChild.selectSingleNode("ip")) Then
                ip = IPAddress.Parse(configDoc.firstChild.selectSingleNode("ip").nodeTypedValue)
            Else
                ip = IPAddress.Parse("127.0.0.1")
                logger.warn(getName() & ": No ip address specified in config, using 127.0.0.1")
            End If
            ' check for the port
            If Not IsNothing(configDoc.firstChild.selectSingleNode("port")) AndAlso IsNumeric(configDoc.firstChild.selectSingleNode("port").nodeTypedValue) Then
                port = Integer.Parse(configDoc.firstChild.selectSingleNode("port").nodeTypedValue)
            Else
                logger.warn(getName() & ": No port number specified in config, using 6000")
                port = 6000
            End If
        Else
            'well there aren't any parameters specified, so we will give some standards
            logger.warn(getName() & ": No ip and port specified in config, using 127.0.0.1:6000")
            ip = IPAddress.Parse("127.10.0.1")
            port = 6000
        End If
    End Sub
End Class


'' Will run as own thread and listen for incoming messages
Friend Class simpleNetwork_Monitor

    Private clients As List(Of Sockets.Socket)
    Private device As simpleNetwork

    Public Sub New(ByRef clients As List(Of Sockets.Socket), ByRef device As simpleNetwork)
        Me.clients = clients
        Me.device = device
    End Sub

    ' Here we are checking for incoming messages in a infinite loop
    Public Sub monitor()
        Dim buffer As Byte()
        Dim message As String
        While True 'loop until thread stops
            For Each socket As Sockets.Socket In clients
                If (socket.Available > 21) Then ' the smallest valid message has a length of 22, so wait until we have this
                    ReDim buffer(socket.Available)
                    socket.Receive(buffer, Sockets.SocketFlags.None)
                    message = System.Text.ASCIIEncoding.ASCII.GetString(buffer)
                    logger.debug(device.getName & ": Received data: " & message)
                    If Not evaluateXML(message) Then
                        'The message was not valid, so we inform the client 
                        ' that we igored it
                        socket.Send(Text.ASCIIEncoding.ASCII.GetBytes("<mesg>Sorry, could not validate your message: " & vbCrLf & vbTab & message & "Ignoring it!</mesg>" & vbCrLf), Sockets.SocketFlags.None)
                    End If
                End If
            Next
            Thread.Sleep(10)
        End While
    End Sub

    ' Here we're checking if the message is correct and if so
    ' changing the inputs depending on it
    ' and return true
    Private Function evaluateXML(ByVal xml As String) As Boolean

        'load xml to a parser and get the channel tag
        Dim messageDoc = New MSXML2.DOMDocument
        messageDoc.loadXML(xml)
        Dim channelNode As IXMLDOMElement = messageDoc.selectSingleNode("channel")

        ' if a channel tag is present, check if it is valid (contains a nr or name attribute)
        ' and try to parse the state as boolean
        If Not IsNothing(channelNode) Then
            Dim newState As Boolean
            Dim strState As String = channelNode.nodeTypedValue.ToString
            If strState = "true" OrElse strState = "1" OrElse strState = "on" OrElse strState = "active" OrElse strState = "yes" Then
                newState = True
            ElseIf strState = "false" OrElse strState = "0" OrElse strState = "off" OrElse strState = "inactive" OrElse strState = "no" Then
                newState = False
            Else
                logger.debug(device.getName & ": Could not parse state: " & channelNode.nodeTypedValue & ". Will ignore message: " & xml)
                Return False
            End If
            logger.debug(device.getName & ": has valid state: " & newState.ToString)

            ' check if nr is present first and if channel is present, set the state
            ' if nr wasn't there, check the same for name. If both are missing, the messages is invalid
            If Not IsDBNull(channelNode.getAttribute("nr")) AndAlso IsNumeric(channelNode.getAttribute("nr")) AndAlso Not IsNothing(device.getInputByChannel(channelNode.getAttribute("nr"))) Then
                device.getInputByChannel(channelNode.getAttribute("nr")).changeState(newState)
                Return True
            ElseIf Not IsDBNull(channelNode.getAttribute("name")) AndAlso Not IsNothing(device.getInputByName(channelNode.getAttribute("name").ToString)) Then
                device.getInputByName(channelNode.getAttribute("name")).changeState(newState)
                Return True
            Else
                logger.debug(device.getName & ": No channel identifier specified in <channel> tag. Will ignore message: " & xml)
            End If
        Else
            logger.debug(device.getName & ": No <channel> defined in message. Will ignore message: " & xml)
        End If
        Return False
    End Function

End Class


'' This class will wait for incoming client connections and handle them
Friend Class connection_Monitor

    Private socket As Sockets.Socket
    Private clients As List(Of Sockets.Socket)
    Private device As simpleNetwork

    Public Sub New(ByRef socket As Sockets.Socket, ByRef clients As List(Of Sockets.Socket), ByRef device As simpleNetwork)
        Me.socket = socket
        Me.clients = clients
        Me.device = device
    End Sub

    '' In here we will listen at the given port for incoming clientconnections
    '' and handle them
    Public Sub listen()
        Dim newClient As Sockets.Socket
        Dim info As IPEndPoint
        While True
            Try
                ' Accept new client
                newClient = socket.Accept()

                ' get info about client for logging
                info = newClient.RemoteEndPoint
                logger.debug(device.getName & ": New client connected: " & info.Address.ToString & ":" & info.Port.ToString)

                'Now that we are connected, lets give the client some info
                'send a Hello and all inputs / output states once
                newClient.Send(System.Text.ASCIIEncoding.ASCII.GetBytes("<mesg>Welcome to TallyProcessor device " & device.getName & "!</mesg>" & vbCrLf), Sockets.SocketFlags.None)
                If device.getOutChannels > 0 Then
                    newClient.Send(System.Text.ASCIIEncoding.ASCII.GetBytes("<mesg>You are listening to the following outputs:</mesg>" & vbCrLf), Sockets.SocketFlags.None)
                    For Each output As output In device.getOutputs
                        newClient.Send(device.getMessage(output), Sockets.SocketFlags.None)
                    Next
                End If
                If device.getInChannels > 0 Then
                    newClient.Send(System.Text.ASCIIEncoding.ASCII.GetBytes("<mesg>These are the inputs you may want to change:</mesg>" & vbCrLf), Sockets.SocketFlags.None)
                    For Each input As input In device.getInputs
                        newClient.Send(device.getMessage(input), Sockets.SocketFlags.None)
                    Next
                End If

                'The connection is established 
                'so add the client to the list so that it will receive data
                clients.Add(newClient)
            Catch e As Exception
                logger.debug(device.getName & ": Error while listening for incoming connections.")
                logger.debug(device.getName & ": " & e.Message)
            End Try
        End While
    End Sub
End Class
