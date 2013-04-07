Imports System.Threading

Public Class k8055D
    Inherits device
    Implements IDevice

    ' Alle nötigen dll imports
    Private Declare Function OpenDevice Lib "k8055d.dll" (ByVal CardAddress As Integer) As Integer
    Private Declare Sub CloseDevice Lib "k8055d.dll" ()
    Private Declare Function SearchDevices Lib "k8055d.dll" () As Integer
    Private Declare Sub WriteAllDigital Lib "k8055d.dll" (ByVal Data As Integer)
    Private Declare Sub ClearDigitalChannel Lib "k8055d.dll" (ByVal Channel As Integer)
    Private Declare Sub ClearAllDigital Lib "k8055d.dll" ()
    Private Declare Sub SetDigitalChannel Lib "k8055d.dll" (ByVal Channel As Integer)
    Private Declare Sub SetAllDigital Lib "k8055d.dll" ()
    Private Declare Function ReadDigitalChannel Lib "k8055d.dll" (ByVal Channel As Integer) As Boolean
    Private Declare Function ReadAllDigital Lib "k8055d.dll" () As Integer

    'Interne Variablen
    Private devAddress As Integer
    Private initState As Boolean
    Private inputThread As Thread
    Private monitorThread As Thread
    Private monitor As k8055d_Monitor
    Private worker As k8055d_Worker
    Private pause As Integer

    Public Sub New(ByVal name As String, ByVal config As String)
        MyBase.new(name, config)
        'inputListener = New List(Of IInputChangedListener)
        logger.debug(getName() & ": New k8055d class instance named '" & name & "' created")
    End Sub

    Public Overrides Function init() As Boolean
        ' Initialisiere das USB Board
        '============================

        logger.debug(getName() & ": Initializing device '" & getName() & "'")

        ' Config auswerten und einlesen
        config(getConfig())

        ' USB Board offnen
        logger.debug(getName() & ": Try to open usb connection to board " & devAddress & "...")
        Dim i = OpenDevice(devAddress)
        If i <> devAddress Then
            logger.warn(getName() & ": Couldn't connect to board " & devAddress & ". Device '" & "' is not initialized")
            initState = False
            Return False
        End If
        logger.debug(getName() & ": ...connected.")
        logger.debug(getName() & ": Start connection monitoring...")
        monitor = New k8055d_Monitor(devAddress, 3000, getOutputs, getName)
        monitorThread = New Thread(AddressOf monitor.checkConnection)
        monitorThread.Start()

        ' Thread für Inputpolling
        logger.debug(getName() & ": Starting inputpolling...")
        If getInChannels() > 0 Then
            worker = New k8055d_Worker(pause, Me)
            inputThread = New Thread(AddressOf worker.updateInputs)
            inputThread.Start()
        End If
        logger.debug(getName() & ": ...done.")

        'Einmal alle ausgänge updaten
        For Each output As output In getOutputs()
            hasChanged(output)
        Next
        initState = True
        logger.debug(getName() & ": Device '" & getName() & "' is initialized.")
        Return initState
    End Function

    Public Overloads Sub close()
        If isInitialized() Then
            If Not IsNothing(inputThread) Then inputThread.Abort()
            If Not IsNothing(monitorThread) Then monitorThread.Abort()
        End If
    End Sub

    Public Overrides Function isInitialized() As Boolean
        Return initState
    End Function

    Public Overrides Sub hasChanged(ByRef output As output)
        logger.debug(output.ToString & " gave change notifycation.")
        setOutput(output.getDeviceChannel, output.getState())
    End Sub

    Private Sub config(ByVal config As String)
        ' Standardwerte
        devAddress = 0
        pause = 10

        ' XML Verarbeiten
        Dim configDoc As New MSXML2.DOMDocument
        configDoc.loadXML(config)
        If configDoc.hasChildNodes Then
            If Not IsNothing(configDoc.firstChild.selectSingleNode("cardaddress")) Then
                devAddress = configDoc.firstChild.selectSingleNode("cardaddress").nodeTypedValue
            End If
            If Not IsNothing(configDoc.firstChild.selectSingleNode("pollingrate")) Then
                pause = configDoc.firstChild.selectSingleNode("pollingrate").nodeTypedValue
            End If
        End If
    End Sub

    Private Sub setOutput(ByVal channel As Integer, ByVal state As Boolean)
        If state Then
            SetDigitalChannel(channel)
        Else
            ClearDigitalChannel(channel)
        End If
    End Sub
End Class

Friend Class k8055d_Worker

    Private Declare Function ReadDigitalChannel Lib "k8055d.dll" (ByVal Channel As Integer) As Boolean
    Private Declare Function ReadAllDigital Lib "k8055d.dll" () As Integer
    Private pause As Integer
    Private device As k8055D

    Public Sub New(ByVal pause As Integer, ByRef device As k8055D)
        Me.device = device
        Me.pause = pause
    End Sub

    Public Sub updateInputs()
        While True
            For Each input As input In device.getInputs
                input.changeState(ReadDigitalChannel(input.getDeviceChannel))
            Next
            Thread.Sleep(pause)
        End While
    End Sub
End Class

Friend Class k8055d_Monitor
    Private Declare Function SearchDevices Lib "k8055d.dll" () As Integer
    Private Declare Function OpenDevice Lib "k8055d.dll" (ByVal CardAddress As Integer) As Integer

    Private cardaddress As Integer
    Private pause As Integer
    Private outputs As List(Of output)
    Private name As String

    Public Sub New(ByVal cardaddress As Integer, ByVal pause As Integer, ByRef outputs As List(Of output), ByVal name As String)
        Me.cardaddress = cardaddress
        Me.pause = pause
        Me.outputs = outputs
        Me.name = name
    End Sub

    Public Sub checkConnection()
        Dim connected As Boolean
        While True
            If SearchDevices() = 0 Then
                connected = False
                logger.err(name & ": Lost connection to k8055d. Try to reconnect...")
            End If
            If Not connected Then
                OpenDevice(cardaddress)
                If SearchDevices() > 0 Then
                    connected = True
                    'Einmal alle ausgänge updaten
                    For Each output As output In outputs
                        output.getDevice.hasChanged(output)
                    Next
                End If
            End If
            Thread.Sleep(pause)
        End While
    End Sub
End Class
