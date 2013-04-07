''' <summary>
''' This Interface defines everything needed by input or output devices and has to be implement by them.
''' </summary>
Public Interface IDevice
    Sub hasChanged(ByRef output As output)

    Sub addInput(ByRef input As input)
    Sub addOutput(ByRef output As output)

    Function init() As Boolean
    Sub close()
    Function isInitialized() As Boolean
    Function getName() As String
    Function getConfig() As String
    Function getInChannels() As Integer
    Function getOutChannels() As Integer
    Function getOutputs() As List(Of output)
    Function getInputs() As List(Of input)
    Function getOutputByName(ByVal name As String) As output
    Function getOutputByChannel(ByVal channel As Integer) As output
    Function getOutputByDeviceChannel(ByVal channel As Integer) As output
    Function getInputByName(ByVal name As String) As input
    Function getInputByChannel(ByVal channel As Integer) As input
    Function getInputByDeviceChannel(ByVal channel As Integer) As input



End Interface

