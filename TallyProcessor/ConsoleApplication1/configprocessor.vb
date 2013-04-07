''' <summary>
''' The configprocessor reads a xml config file and generates devices, inputs, output and their expressions as given in the config.
''' </summary>

Public Class configprocessor

    Private configDOC As MSXML2.DOMDocument
    Private configFile As String
    Private devices As Dictionary(Of String, IDevice) ' devices by name
    Private inputByName As Dictionary(Of String, input)
    Private inputByNr As Dictionary(Of Integer, input)
    Private outputByName As Dictionary(Of String, output)
    Private outputByNr As Dictionary(Of Integer, output)
    Const xsdFile As String = "config.xsd"


    Public Sub New(ByVal configFile As String)
        logger.debug("Initializing configuration...")
        MyClass.configFile = configFile
        logger.debug("Config file: " & configFile)
        devices = New Dictionary(Of String, IDevice)
        inputByName = New Dictionary(Of String, input)
        inputByNr = New Dictionary(Of Integer, input)
        outputByName = New Dictionary(Of String, output)
        outputByNr = New Dictionary(Of Integer, output)
    End Sub

    Public Function open() As Boolean
        logger.log("Open file..")
        configDOC = New MSXML2.DOMDocument60
        Dim xsdSchema As New MSXML2.XMLSchemaCache60

        ' check for xsd file to exists
        If My.Computer.FileSystem.FileExists(xsdFile) Then
            logger.debug("Valdiating config file against " & xsdFile)
            xsdSchema.add("", xsdFile)
            configDOC.schemas = xsdSchema
            configDOC.validateOnParse = True
        Else
            logger.warn("Could not load " & xsdFile & " to valdiate config file. This may lead to program crashes with faulty config files.")
            configDOC.validateOnParse = False
        End If


        If configDOC.load(configFile) Then
            logger.log("...file opend.")

            ' remove stdLogger and add configured logger
            logger.removeLogAction(logger.stdLogAction)
            If configDOC.getElementsByTagName("logging").length > 0 Then
                logger.config(configDOC.getElementsByTagName("logging").nextNode.xml)
            End If
            'removing comments
            For Each comment As MSXML2.IXMLDOMNode In configDOC.selectNodes("//comment()")
                comment.parentNode.removeChild(comment)
            Next

            logger.debug("XML DUMP:" & vbNewLine & configDOC.xml)

            configDevices()
            Dim initOK As Boolean = True
            For Each dev As IDevice In devices.Values
                initOK = initOK AndAlso dev.init()
            Next
            Return initOK
        Else
            With configDOC.parseError
                logger.critical("Error loading config file " & configFile & " at " & .line & ":" & .linepos & " code " & .errorCode & ": " & .reason & vbNewLine & .srcText)
            End With
            Return False
        End If
    End Function

    Public Sub close()
        For Each dev As IDevice In devices.Values
            dev.close()
        Next
    End Sub

    Private Sub configDevices()
        logger.log("Initializing devices...")
        Dim nodes = configDOC.getElementsByTagName("device")
        Dim node As MSXML2.IXMLDOMNode
        Dim dev As IDevice

        If nodes.length = 0 Then
            logger.warn("No device specified. TallyProcessor will do nothing.")
        Else
            ' Create devices with inputs and outputs
            For Each node In nodes
                If Not IsNothing(node) Then
                    logger.debug("Proccesing next device node:")
                    dev = deviceFactory(node)
                    If Not IsNothing(dev) Then devices.Add(dev.getName, dev)
                Else
                    logger.err("Found empty device node.")
                End If
            Next

            ' Expressions setzten und den inputs ihre outputs zuweisen
            Dim exp As expression
            For Each output In outputByName.Values
                logger.debug("Computing output expressions for '" & output.ToString & "'")
                exp = getExpression(output.getMappingNode)
                If IsNothing(exp) Then
                    output.setExpression(New expression(False))
                    logger.err("Couldn't compute valid output expression for '" & output.ToString & "'. Will set it to false forever. Please check your configuration.")
                Else
                    output.setExpression(exp)
                    logger.debug(output.getExpression.ToString)
                End If

                ' Allen inputs die in der Expression des outputs sind sagen dass sie diesen output benachrichtigen müssen
                For Each input As input In output.getExpression.getInputs
                    input.addOutput(output)
                Next
            Next
        End If
    End Sub

    Private Function deviceFactory(ByRef devNode As MSXML2.IXMLDOMNode) As IDevice
        Dim name As String
        Dim xml = ""
        Dim classType As String
        Dim iC As Integer
        Dim oC As Integer
        Dim inputs As MSXML2.IXMLDOMNodeList
        Dim outputs As MSXML2.IXMLDOMNodeList
        Dim device As IDevice

        ' Alle Bestandteile der device Spezifikation auslesen
        name = devNode.selectSingleNode("name").nodeTypedValue
        classType = devNode.selectSingleNode("class").nodeTypedValue
        xml = devNode.selectSingleNode("parameter").xml
        iC = devNode.selectSingleNode("inChannels").nodeTypedValue
        oC = devNode.selectSingleNode("outChannels").nodeTypedValue
        If iC > 0 Then inputs = devNode.selectSingleNode("channels").selectSingleNode("input").selectNodes("channel")
        If oC > 0 Then outputs = devNode.selectSingleNode("channels").selectSingleNode("output").selectNodes("channel")

        'Fehlt etwas entscheidendes
        If IsNothing(name) Then
            Throw New Exception("Error parsing input file. Device NAME paramter not specified. " & devNode.xml)
        ElseIf IsNothing(classType) Then
            Throw New Exception("Error parsing input file. Device CLASS paramter not specified. " & devNode.xml)
        ElseIf IsNothing(inputs) AndAlso IsNothing(outputs) Then
            logger.err("Error parsing input file. No channels specified. " & devNode.xml)
        End If

        'dev der richtigen klasse erstellen
        logger.debug("Trying to create device '" & name & "' of class '" & classType & "'")

        ' Such nach einem passenden Type im Namesspace von TallyProcessor
        Dim t As Type = Type.GetType("TallyProcessor." & classType)
        If IsNothing(t) Then
            logger.err("Unkown device class '" & classType & "'. Can't configure device!")
            Return Nothing
        Else
            device = Activator.CreateInstance(t, New Object() {name, xml})
            logger.debug("Succesfully created device '" & name & "' of class '" & classType & "'")
        End If

        ' Device Parameter ausgeben
        For Each par As MSXML2.IXMLDOMNode In devNode.selectSingleNode("parameter").childNodes
            logger.debug(vbTab & "Parameter: " & vbTab & par.nodeName & " = " & par.nodeTypedValue)
        Next

        'Inputs erstellen
        Dim input As input
        If iC > 0 Then
            For Each inputNode As MSXML2.IXMLDOMElement In inputs
                logger.log(vbTab & "Input Nr. " & inputNode.getAttribute("nr") & ": " & vbTab & inputNode.getAttribute("name"))
                If inputByNr.ContainsKey(inputNode.getAttribute("nr")) Then
                    logger.warn("Error reading input configuration. Input Nr. " & inputNode.getAttribute("nr") & " already exists. See" & vbNewLine & inputByNr.Item(inputNode.getAttribute("nr")).ToString & vbNewLine & "Ignoring input!")
                Else
                    input = New input(inputNode.getAttribute("name"), device, inputNode.getAttribute("nr"), New expression(False))
                    input.getExpression.setInput(input)
                    device.addInput(input)
                    registerInput(input)
                    logger.debug("Added '" & input.ToString & "'")
                End If
            Next
        End If

        ' Outputs erstellen (ohne Expressions)
        Dim output As output
        If oC > 0 Then
            For Each outputNode As MSXML2.IXMLDOMElement In outputs
                logger.log(vbTab & "output Nr. " & outputNode.getAttribute("nr") & ": " & vbTab & outputNode.getAttribute("name"))
                If outputByNr.ContainsKey(outputNode.getAttribute("nr")) Then
                    logger.warn("Error reading output configuration. output Nr. " & outputNode.getAttribute("nr") & " already exists. See" & vbNewLine & outputByNr.Item(outputNode.getAttribute("nr")).ToString & vbNewLine & "Ignoring output!")
                Else
                    output = New output(outputNode.getAttribute("name"), device, outputNode.getAttribute("nr"), outputNode.selectSingleNode("mapping"))
                    device.addOutput(output)
                    registerOutput(output)
                    logger.debug("Added '" & output.ToString & "'")
                End If
            Next
        End If
        Return device
    End Function

    Private Function getExpression(ByRef mappingNode As MSXML2.IXMLDOMElement) As expression
        'On Error Resume Next

        If mappingNode Is Nothing Then
            Return Nothing
        Else
            Select Case mappingNode.nodeName
                Case "mapping"
                    'Entweder ein operator oder ein input muss folgen
                    If mappingNode.hasChildNodes Then
                        Dim exp1 As expression = getExpression(mappingNode.firstChild())
                        If IsNothing(exp1) Then
                            logger.err("Empty or faulty output mapping found. This output will be false forever!")
                            Return New expression(False)
                        Else
                            Return exp1
                        End If
                    Else
                        logger.warn("Empty output mapping found. This output will be false forever!")
                        Return New expression(False)
                    End If

                Case "operator"
                    ' Ein Operator gefunden. Kann 1 (NOT), 2 (oder mehr Kinder haben --> geht noch nicht)
                    If mappingNode.hasChildNodes Then
                        Dim op = mappingNode.getAttribute("type")
                        Dim operand1 As expression = Nothing
                        Dim operand2 As expression = Nothing

                        If UCase(op) = "NOT" Then
                            If mappingNode.childNodes.length <> 1 Then
                                logger.critical("Illegal mapping definition. NOT must not have more or less than one operand.")
                                logger.critical("XML Dump of invalid tags: " & vbNewLine & mappingNode.xml)
                                Throw New InvalidExpressionException
                            Else
                                operand1 = getExpression(mappingNode.firstChild)
                            End If
                        Else
                            If mappingNode.childNodes.length <> 2 Then
                                logger.critical("Illegal mapping definition. " & UCase(op) & " must not have more or less than two operands.")
                                logger.critical("XML Dump of invalid tags: " & vbNewLine & mappingNode.xml)
                                Throw New InvalidExpressionException
                            Else
                                operand1 = getExpression(mappingNode.firstChild)
                                operand2 = getExpression(mappingNode.firstChild.nextSibling)
                            End If
                        End If
                        If IsNothing(operand1) Then
                            logger.err("Empty or faulty operator inputs found. This operand will be false forever!")
                            Return New expression(False)
                        Else
                            Return New expression(operand1, operand2, UCase(op))
                        End If
                    Else
                        Throw New Exception("Error parsing output mapping. <operator></operator> has to contain at least one element.")
                    End If
                Case "input"
                    ' Es kann nur genau einen input geben, etweder vom typ boolean oder vom typ nr/name der ein gültiger input sein muss
                    Select Case LCase(mappingNode.getAttribute("type"))
                        Case "boolean"
                            If LCase(mappingNode.nodeTypedValue) = "true" Then
                                Return New expression(True)
                            Else
                                Return New expression(False)
                            End If
                        Case "nr"
                            If IsNumeric(mappingNode.nodeTypedValue) AndAlso inputByNr.ContainsKey(Integer.Parse(mappingNode.nodeTypedValue)) Then
                                Return inputByNr.Item(Integer.Parse(mappingNode.nodeTypedValue)).getExpression
                            Else
                                logger.err("Error parsing output mapping. Input: '" & mappingNode.nodeTypedValue & "' does not exists or isn't a number. Ignoring this input for output expression. This may lead to strange output results.")
                                Return Nothing
                            End If
                        Case "name"
                            If inputByName.ContainsKey(mappingNode.nodeTypedValue) Then
                                Return inputByName.Item(mappingNode.nodeTypedValue).getExpression
                            Else
                                logger.err("Error parsing output mapping. Input: '" & mappingNode.nodeTypedValue & "' does not exists. Ignoring this input for output expression. This may lead to strange output results.")
                                Return Nothing
                            End If
                        Case Else
                            Throw New Exception("Error parsing output mapping. Unknown input: " & mappingNode.nodeTypedValue)
                            Return Nothing
                    End Select
                Case Else
                    Throw New Exception("Error parsing output mapping. Unknown element " & mappingNode.xml)
            End Select
        End If
    End Function


    Private Function registerInput(ByRef input As input) As Boolean
        If inputByName.ContainsKey(input.getName) OrElse inputByNr.ContainsKey(input.getChannel) Then
            Return False
        Else
            inputByName.Add(input.getName, input)
            inputByNr.Add(input.getChannel, input)
            Return True
        End If
    End Function

    Private Function registerOutput(ByRef output As output) As Boolean
        If outputByName.ContainsKey(output.getName) OrElse outputByNr.ContainsKey(output.getChannel) Then
            Return False
        Else
            outputByName.Add(output.getName, output)
            outputByNr.Add(output.getChannel, output)
            Return True
        End If
    End Function

End Class
