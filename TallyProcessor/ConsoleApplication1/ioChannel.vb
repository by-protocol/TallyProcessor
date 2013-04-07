'''<summary>
''' ioChannel:  Maybe the main class of the tallyprocessor. It holds the evaluating logic for input and output channels.
'''              The pipeline is as as follows:
'''                  INIT:   Depending on the config file, a number of In/Output devices are made and their inputs registered to.
'''                          Afterwards, the output expressions of each output channel are computed 
'''                          and registered to the outputs and all inputs they depend on.
'''                  RUN:    The inputdevice triggers a changeState to the inputchannel and the input notifys all depending 
'''                          outputs to reevaluate their state. If changed, the output notifys its device that it has changed.
'''                          The implementation of device does what ever is needed to handel the new state.
'''                  SCHEMA: input change at device --> device changes inputState --> input notifys depending outputs --> 
'''                          output reevaluates its state --> output notifys its device that it hasChanged --> device does what ever is needed here 
'''</summary>

Public Class ioChannel
    Private name As String
    Private state As expression
    Private gChannel As Integer
    Private iChannel As Integer
    Private dev As IDevice

    ' Constructor for a new ioClass which is an input or output channel
    ' parameter:
    '       name    -   then name of the channel
    '       device  -   the device of this channel
    '       channel -   the global channelnumber of this channel
    '       state   -   the state expression of this channel which is eighter a boolean for inputs or 
    '                   a complex expression for outputs to evaluate the output state depending on inputs
    Public Sub New(ByVal name As String, ByRef device As IDevice, ByVal channel As Integer, ByRef expression As expression)
        MyClass.name = name
        dev = device
        gChannel = channel
        MyClass.state = expression
    End Sub

    ' Sets the globle channelnumber of this channel
    Public Sub setChannel(ByVal channel As Integer)
        gChannel = channel
    End Sub

    ' Sets the local, device based channelnumber of this channel
    Public Sub setDeviceChannel(ByVal channel As Integer)
        iChannel = channel
    End Sub

    ' Sets the expression related to this channel which is just a boolean for inputs or a complex expression for outputs
    Public Sub setExpression(ByRef expression As expression)
        state = expression
    End Sub

    ' Triggers a change to the channels state
    Public Sub changeState(ByVal newState As Boolean)
        logger.debug(getName() & ": Got changeState event. Changing from '" & getState() & "' to '" & newState & "'")
        If getState() <> newState Then
            state.changeState(newState)
        End If
    End Sub

    ' Returns the channels state
    Public Function getState() As Boolean
        Return state.eval()
    End Function

    ' Returns the expression related to this channel which is just a boolean for inputs or a complex expression for outputs
    Public Function getExpression() As expression
        Return state
    End Function

    Public Function getChannel() As Integer
        Return gChannel
    End Function

    Public Function getDeviceChannel() As Integer
        Return iChannel
    End Function

    ' Returns the name of this channels as given by the config.xml
    Public Function getName() As String
        Return name
    End Function

    ' Returns the device this channel belongs to
    Public Function getDevice() As IDevice
        Return dev
    End Function

    Public Overrides Function ToString() As String
        Return getDevice().getName() & "." & getName()
    End Function
End Class
