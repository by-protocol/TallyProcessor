Public MustInherit Class device
    Implements IDevice

    Private inChannels As Integer
    Private outChannels As Integer
    Private name As String
    Private config As String
    Private outputs As List(Of output)
    Private inputs As List(Of input)

    Public Sub New(ByVal name As String, ByVal config As String)
        MyClass.name = name
        MyClass.config = config
        inputs = New List(Of input)
        outputs = New List(Of output)
    End Sub

    Public Function getOutputByName(ByVal name As String) As output Implements IDevice.getOutputByName
        For Each output As output In outputs
            If output.getName = name Then
                Return output
            End If
        Next
        Throw New Exception("No output named " & name & " found")
    End Function

    Public Function getOutputByChannel(ByVal channel As Integer) As output Implements IDevice.getOutputByChannel
        For Each output As output In outputs
            If output.getChannel = channel Then
                Return output
            End If
        Next
        Throw New Exception("No output associated to channel " & channel & " found")
    End Function

    Public Function getOutputByDeviceChannel(ByVal channel As Integer) As output Implements IDevice.getOutputByDeviceChannel
        If outputs.Count <= channel Then
            Return outputs.Item(channel - 1)
        Else
            Throw New Exception("No output associated to channel " & channel & " at this device found")
        End If
    End Function

    Public Function getInputByName(ByVal name As String) As input Implements IDevice.getInputByName
        For Each input As input In inputs
            If input.getName = name Then
                Return input
            End If
        Next
        Throw New Exception("No input named " & name & " found")
    End Function

    Public Function getInputByChannel(ByVal channel As Integer) As input Implements IDevice.getInputByChannel
        For Each input As input In inputs
            If input.getChannel = channel Then
                Return input
            End If
        Next
        Throw New Exception("No input associated to channel " & channel & " found")
    End Function

    Public Function getInputByDeviceChannel(ByVal channel As Integer) As input Implements IDevice.getInputByDeviceChannel
        If inputs.Count <= channel Then
            Return inputs.Item(channel - 1)
        Else
            Throw New Exception("No input associated to channel " & channel & " at this device found")
        End If
    End Function

    Public Sub addOutput(ByRef output As output) Implements IDevice.addOutput
        outChannels = outChannels + 1
        output.setDeviceChannel(outChannels)
        outputs.Add(output)
    End Sub

    Public Sub addInput(ByRef input As input) Implements IDevice.addInput
        inChannels = inChannels + 1
        input.setDeviceChannel(inChannels)
        inputs.Add(input)
    End Sub

    Public Function getName() As String Implements IDevice.getName
        Return name
    End Function

    Public Function getConfig() As String Implements IDevice.getConfig
        Return config
    End Function

    Public Function getInChannels() As Integer Implements IDevice.getInChannels
        Return inChannels
    End Function

    Public Function getOutChannels() As Integer Implements IDevice.getOutChannels
        Return outChannels
    End Function

    Public Function getOutputs() As List(Of output) Implements IDevice.getOutputs
        Return outputs
    End Function

    Public Function getInputs() As List(Of input) Implements IDevice.getInputs
        Return inputs
    End Function

    Public Sub close() Implements IDevice.close
        'Empty
    End Sub

    Public Overridable Sub hasChanged(ByRef output As output) Implements IDevice.hasChanged
        'empty
    End Sub

    Public MustOverride Function init() As Boolean Implements IDevice.init

    Public MustOverride Function isInitialized() As Boolean Implements IDevice.isInitialized
End Class
