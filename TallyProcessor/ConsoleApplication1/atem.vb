Imports BMDSwitcherAPI
Imports System.Threading
Imports System.Runtime.InteropServices

''' <summary>
''' This class works as input connector to get tally from BlackmagicDesign ATEM Switchers.
''' </summary>
''' <remarks></remarks>

Public Class atem
    Inherits device
    Implements IDevice

    'Interne Variablen
    Private ip As String
    Private initState As Boolean
    Private inputThread As Thread
    Private worker As atem_Worker

    Public Sub New(ByVal name As String, ByVal config As String)
        MyBase.new(name, config)
        logger.debug(getName() & ": New atem class instance named '" & name & "' created")
    End Sub

    Public Overrides Function init() As Boolean
        ' Initialisiere ATEM Mixer
        '============================

        logger.debug(getName() & ": Initializing device '" & getName() & "'")

        ' Config auswerten und einlesen
        'config(getConfig())

        ' Thread für Inputpolling
        logger.debug(getName() & ": Starting input monitor...")
        If getInChannels() > 0 Then
            worker = New atem_Worker(Me)
            inputThread = New Thread(AddressOf worker.Main)
            inputThread.Start()
        End If
        logger.debug(getName() & ": ...done.")

        initState = True
        logger.debug(getName() & ": Device '" & getName() & "' is initialized.")
        Return initState
    End Function

    Public Overloads Sub close()
        worker.close()
        inputThread.Abort()
    End Sub

    Public Overrides Function isInitialized() As Boolean
        Return initState
    End Function

    Public Overrides Sub hasChanged(ByRef output As output)
        logger.warn(getName() & ": This device can't handle outputs. Ignoring hasChanged() event.")
    End Sub
End Class

Friend Class atem_Worker
    Implements BMDSwitcherAPI.IBMDSwitcherMixEffectBlockCallback
    Implements BMDSwitcherAPI.IBMDSwitcherCallback

    Private name As String = ""
    Private device As atem
    Private modell As String
    Private mixEfxBlk As IBMDSwitcherMixEffectBlock
    Private switcher As IBMDSwitcher
    Private ip As String = "192.168.10.10"

    Private lookupTable As doubleKeyedDictionary(Of Long, String, input)

    Private liveInput As Long
    Private prewInput As Long
    Private cuedString As String = "_cued"


    Public Sub New(ByRef device As atem)
        setIP(ip)
        Me.name = device.getName
        Me.device = device
        initLookupTable()
    End Sub
    Private Sub initLookupTable()
        lookupTable = New doubleKeyedDictionary(Of Long, String, input)
        For Each input As input In device.getInputs
            lookupTable.addByKey2(input.getName, input)
        Next
    End Sub

    Public Sub Main()

        ' Config auswerten und einlesen
        config(device.getConfig())

        switcher = openATEM(ip)

        ' Switcher init:
        '---------------
        switcher.GetString(_BMDSwitcherPropertyId.bmdSwitcherPropertyIdProductName, modell)
        logger.debug(name & ": Switcher modell " & modell & " detected.")
        switcher.AddCallback(Me)


        ' Mixer init:
        '------------
        Dim pgid, prid As String
        mixEfxBlk = getMixEffectBlock()
        mixEfxBlk.GetInt(_BMDSwitcherMixEffectBlockPropertyId.bmdSwitcherMixEffectBlockPropertyIdProgramInput, pgid)
        mixEfxBlk.GetInt(_BMDSwitcherMixEffectBlockPropertyId.bmdSwitcherMixEffectBlockPropertyIdPreviewInput, prid)
        mixEfxBlk.AddCallback(Me)


        ' Input init:
        '------------
        Dim inputIterator As IBMDSwitcherInputIterator
        Dim ptr As System.IntPtr = New IntPtr()
        Dim guid As System.Guid = System.Guid.Parse("92AB7A73-C6F6-47FC-92A7-C8EEADC9EAAC")
        switcher.CreateIterator(guid, ptr)
        inputIterator = Marshal.GetObjectForIUnknown(ptr)

        Dim input As IBMDSwitcherInput
        Dim lname As String
        Dim sname As String
        Dim preLive As Integer
        Do
            inputIterator.Next(input)
            If Not IsNothing(input) Then
                input.GetString(_BMDSwitcherInputPropertyId.bmdSwitcherInputPropertyIdLongName, lname)
                input.GetString(_BMDSwitcherInputPropertyId.bmdSwitcherInputPropertyIdShortName, sname)
                mixEfxBlk.GetFlag(_BMDSwitcherMixEffectBlockPropertyId.bmdSwitcherMixEffectBlockPropertyIdPreviewLive, preLive)
                Dim id As Long
                input.GetInputId(id)
                Dim live As Boolean = (pgid = id) OrElse (preLive = 1 AndAlso prid = id)
                'logger.debug(name & ": Input " & sname & " found. Live: " & live)
                If lookupTable.containsKey2(lname) Then
                    lookupTable.appendKey1(id, lname)
                    lookupTable.item(id, lname).changeState(live)
                    ' Adding cued of this input as own channel with negative id
                    lookupTable.appendKey1(-1 * (id + 1), lname & cuedString)
                    lookupTable.item(-1 * (id + 1), lname & cuedString).changeState(prid = id)
                    logger.debug(name & ": Found corresponding atem input id '" & id & "' for '" & lookupTable.item(id, Nothing).getName & "(State:" & live & ")'.")
                ElseIf lookupTable.containsKey2(sname) Then
                    lookupTable.appendKey1(id, sname)
                    lookupTable.item(id, sname).changeState(live)
                    ' Adding cued of this input as own channel with negative id
                    lookupTable.appendKey1(-1 * (id + 1), sname & cuedString)
                    lookupTable.item(-1 * (id + 1), sname & cuedString).changeState(prid = id)
                    logger.debug(name & ": Found corresponding atem input id '" & id & "' for '" & lookupTable.item(id, Nothing).getName & "(State:" & live & ")'.")
                Else
                    lookupTable.add(id, lname, Nothing)
                    lookupTable.add(-1 * (id + 1), lname & cuedString, Nothing)
                    logger.warn(name & ": Found unmanaged atem input id '" & id & "': '" & lname & "(State:" & live & ")'.")
                End If
            End If
        Loop While Not IsNothing(input)

        ' gibt es noch configurierte inputs ohne hardware entsprechung???
        For Each inp As input In device.getInputs
            If Not lookupTable.containsValueDoubleKeyed(inp) Then
                logger.err(name & ": Could not assign configured channel. '" & inp.getName & "' to a real input at '" & modell & "'. It will be false/off for ever.")
            End If
        Next

    End Sub

    Public Sub close()
        If Not IsNothing(mixEfxBlk) Then Marshal.ReleaseComObject(mixEfxBlk)
        If Not IsNothing(switcher) Then Marshal.ReleaseComObject(switcher)
    End Sub

    Private Sub config(ByVal config As String)

        ' XML Verarbeiten
        Dim configDoc As New MSXML2.DOMDocument
        configDoc.loadXML(config)

        If configDoc.hasChildNodes Then
            If Not IsNothing(configDoc.firstChild.selectSingleNode("ip")) Then
                ip = configDoc.firstChild.selectSingleNode("ip").nodeTypedValue
            End If
            If Not IsNothing(configDoc.firstChild.selectSingleNode("cuedPostfix")) Then
                cuedString = configDoc.firstChild.selectSingleNode("cuedPostfix").nodeTypedValue
            End If
        End If
    End Sub

    Public Sub setIP(ByVal ip As String)
        Me.ip = ip
    End Sub

    Private Function openATEM(ByVal ip As String) As IBMDSwitcher
        Dim err As _BMDSwitcherConnectToFailure
        Dim switcherDiscover As CBMDSwitcherDiscovery = New CBMDSwitcherDiscovery()
        Try
            switcherDiscover.ConnectTo(ip, switcher, err)
            If IsNothing(switcher) Then
                Throw New Exception(name & ": Could not connect to switcher")
            End If
            If err = _BMDSwitcherConnectToFailure.bmdSwitcherConnectToFailureNoResponse OrElse err = _BMDSwitcherConnectToFailure.bmdSwitcherConnectToFailureIncompatibleFirmware Then
                Throw New Exception(name & ": Could not connect to switcher")
            Else
                logger.debug(name & ": Connected to " & ip)
            End If
            Return switcher
        Catch e As Exception
            logger.err(name & ": Error connecting to " & ip)
            logger.err(e.Message)
            Thread.Sleep(1000)
            initLookupTable()
            Return openATEM(ip)
        End Try
    End Function

    Private Function getMixEffectBlock() As IBMDSwitcherMixEffectBlock
        Dim mixEfxBlkIterator As IBMDSwitcherMixEffectBlockIterator
        Dim Guid = System.Guid.Parse("930BDE3B-4A78-43D0-8FD3-6E82ABA0E117")
        Dim ptr As IntPtr

        switcher.CreateIterator(Guid, ptr)
        mixEfxBlkIterator = Marshal.GetObjectForIUnknown(ptr)
        mixEfxBlkIterator.Next(mixEfxBlk)
        If IsNothing(mixEfxBlk) Then
            Throw New Exception(name & ": Error, could not get first MixEffectBlock.")
        End If
        Marshal.Release(ptr)
        Marshal.ReleaseComObject(mixEfxBlkIterator)

        Return mixEfxBlk
    End Function

    Private Sub MixerPropertyChanged(ByVal propertyId As BMDSwitcherAPI._BMDSwitcherMixEffectBlockPropertyId) Implements BMDSwitcherAPI.IBMDSwitcherMixEffectBlockCallback.PropertyChanged
        Dim iid As Long
        Select Case propertyId
            Case _BMDSwitcherMixEffectBlockPropertyId.bmdSwitcherMixEffectBlockPropertyIdProgramInput
                mixEfxBlk.GetInt(propertyId, iid)
                If liveInput <> iid Then
                    ' alten live input ausschalten (wenn er als preview live bleibt macht dass das entsprechende event
                    If lookupTable.containsKey1(liveInput) AndAlso Not IsNothing(lookupTable.item(liveInput, Nothing)) Then
                        lookupTable.item(liveInput, Nothing).changeState(False)
                    End If
                    If lookupTable.containsKey1(iid) AndAlso Not IsNothing(lookupTable.item(iid, Nothing)) Then
                        lookupTable.item(iid, Nothing).changeState(True)
                    End If
                    liveInput = iid
                    logger.debug(name & ": Program input changed to '" & lookupTable.getKey2(iid) & "'")
                End If
            Case _BMDSwitcherMixEffectBlockPropertyId.bmdSwitcherMixEffectBlockPropertyIdPreviewInput
                mixEfxBlk.GetInt(propertyId, iid)
                If prewInput <> iid Then
                    ' Set live input state
                    If lookupTable.containsKey1(iid) AndAlso Not IsNothing(lookupTable.item(iid, Nothing)) Then
                        Dim live As Integer
                        mixEfxBlk.GetFlag(_BMDSwitcherMixEffectBlockPropertyId.bmdSwitcherMixEffectBlockPropertyIdPreviewLive, live)
                        lookupTable.item(iid, Nothing).changeState(live = 1)
                    End If

                    ' Set cued input state
                    If lookupTable.containsKey1(-1 * (prewInput + 1)) AndAlso Not IsNothing(lookupTable.item(-1 * (prewInput + 1), Nothing)) Then
                        lookupTable.item(-1 * (prewInput + 1), Nothing).changeState(False)
                    End If
                    If lookupTable.containsKey1(-1 * (1 + iid)) AndAlso Not IsNothing(lookupTable.item(-1 * (1 + iid), Nothing)) Then
                        lookupTable.item(-1 * (1 + iid), Nothing).changeState(True)
                    End If
                    prewInput = iid
                    logger.debug(name & ": Preview input changed to '" & lookupTable.getKey2(iid) & "'")
                End If
            Case _BMDSwitcherMixEffectBlockPropertyId.bmdSwitcherMixEffectBlockPropertyIdPreviewLive
                Dim live As Integer
                mixEfxBlk.GetInt(_BMDSwitcherMixEffectBlockPropertyId.bmdSwitcherMixEffectBlockPropertyIdPreviewInput, iid)
                mixEfxBlk.GetFlag(propertyId, live)
                If iid <> prewInput Then
                    logger.warn(name & ": Actual preview input is not the stored on. Stored: '" & lookupTable.getKey2(prewInput) & "' <> actual: '" & lookupTable.getKey2(iid) & "'")
                End If
                If lookupTable.containsKey1(iid) AndAlso Not IsNothing(lookupTable.item(iid, Nothing)) Then lookupTable.item(iid, Nothing).changeState(live = 1)
                If live = 1 Then
                    logger.debug(name & ": Preview input  '" & lookupTable.getKey2(iid) & "' is live now")
                Else
                    logger.debug(name & ": Preview input  '" & lookupTable.getKey2(iid) & "' is not live anymore")
                End If
        End Select

    End Sub

    Private Sub Disconnected() Implements BMDSwitcherAPI.IBMDSwitcherCallback.Disconnected
        logger.debug(name & ": Disconnected from Atem switcher!!! Try to reconnect...")
        Main()
    End Sub

    Private Sub SwitcherPropertyChanged(ByVal propertyId As BMDSwitcherAPI._BMDSwitcherPropertyId) Implements BMDSwitcherAPI.IBMDSwitcherCallback.PropertyChanged
        logger.debug(name & ": Switcher Property change detected at " & propertyId)
    End Sub
End Class


