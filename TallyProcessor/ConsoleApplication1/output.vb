'''<summary>
''' output: This class inherits all basic input/output functions from ioInterface and implements a new constructor for output channels.
'''</summary>

Public Class output
    Inherits ioChannel

    Private mappingNode As MSXML2.IXMLDOMNode

    Public Sub New(ByVal name As String, ByRef device As IDevice, ByVal channel As Integer, ByRef mappingNode As MSXML2.IXMLDOMNode)
        MyBase.New(name, device, channel, New expression(False))
        MyClass.mappingNode = mappingNode
    End Sub

    Public Function getMappingNode() As MSXML2.IXMLDOMNode
        Return mappingNode
    End Function

    ''' <summary>
    ''' If any input ouf this outputs expression changes, it calls the hasChanged method and triggers
    ''' the output inform its device.
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub hasChanged()
        getDevice.hasChanged(Me)
    End Sub

End Class
