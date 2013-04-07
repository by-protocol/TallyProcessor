'''<summary>
''' input:   This class inherits all basic input/output functions from ioInterface and the interface IInputChangeListener.
'''          Every input channel registers it self to a device. The device will call changeState() and this will trigger 
'''         an update to all outputs related to this input which are stored in myOutputs.
''' </summary>

Public Class input
    Inherits ioChannel
    Private myOutputs As List(Of output)

    Public Sub New(ByVal name As String, ByRef device As IDevice, ByVal channel As Integer, ByRef state As expression)
        MyBase.New(name, device, channel, state)
        myOutputs = New List(Of output)
    End Sub

    Public Sub New(ByVal name As String, ByRef device As IDevice, ByVal channel As Integer, ByRef state As expression, ByRef outputs As List(Of output))
        MyBase.New(name, device, channel, state)
        myOutputs = outputs
    End Sub

    Public Function getOutputs() As List(Of output)
        Return myOutputs
    End Function

    Public Sub setOutputs(ByRef outputs As List(Of output))
        MyClass.myOutputs = outputs
    End Sub

    Public Sub addOutput(ByRef output As output)
        myOutputs.Add(output)
    End Sub

    Public Overloads Sub changeState(ByVal newState As Boolean)
        If getState() <> newState Then
            logger.debug(getName() & ": Changed state from '" & getState() & "' to '" & newState & "'")
            MyBase.changeState(newState)
            updateOutputs()
        End If
    End Sub

    Private Sub updateOutputs()
        logger.debug(getName() & ": Notifing outputs about a changed input")
        For Each output As output In myOutputs
            logger.debug("notify '" & output.ToString & "'")
            output.hasChanged()
        Next
    End Sub

End Class
