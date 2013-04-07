'''<summary>
''' expression: A self evaluating, recursiv boolean expression class.
'''</summary>

Public Class expression
    Private in1, in2 As expression
    Private evOperator As String
    Private State As Boolean
    Private input As input

    ' Operator names in german because the english ones are keywords 
    Public Const UND = "AND"
    Public Const ODER = "OR"
    Public Const NICHT = "NOT"
    Public Const XODER = "XOR"


    Public Sub New(ByVal initState As Boolean)
        State = initState
    End Sub

    Public Sub New(ByRef input1 As expression, ByRef input2 As expression, ByVal evalOperator As String)
        in1 = input1
        in2 = input2
        evOperator = evalOperator
    End Sub

    Public Function eval() As Boolean
        If evOperator = "" Then
            Return State
        Else
            Select Case evOperator
                Case NICHT
                    Return Not in1.eval()
                Case UND
                    Return in1.eval() AndAlso in2.eval()
                Case ODER
                    Return in1.eval() OrElse in2.eval()
                Case XODER
                    Return in1.eval() Xor in2.eval()
                Case Else
                    Throw New Exception("Error evaluating " & evOperator & ". Unknown operator")
            End Select
        End If
    End Function

    Public Function isAtom() As Boolean
        If evOperator = "" Then
            Return True
        Else
            Return False
        End If
    End Function

    Public Function changeState(ByVal newState As Boolean) As Boolean
        If isAtom() Then
            State = newState
            Return True
        Else
            Return False
        End If
    End Function

    Public Sub setInput(ByVal input As input)
        MyClass.input = input
    End Sub

    Public Function getInput() As input
        Return input
    End Function

    Public Function getInputs() As List(Of input)
        Dim inputs As New List(Of input)
        If Not IsNothing(in1) Then
            inputs.AddRange(in1.getInputs)
        End If
        If Not IsNothing(in2) Then
            inputs.AddRange(in2.getInputs)
        End If
        If Not IsNothing(input) Then
            inputs.Add(input)
        End If
        Return inputs
    End Function

    Public Overrides Function ToString() As String
        If isAtom() Then
            If IsNothing(input) Then
                Return "'" & State & "'"
            Else
                Return "'" & input.ToString & "{" & State & "}'"
            End If
        Else
            Select Case evOperator
                Case NICHT
                    Return "(" & NICHT & "(" & in1.ToString & "))"
                Case Else
                    Return "(" & in1.ToString & " " & evOperator & " " & in2.ToString & ")"
            End Select
        End If
    End Function
End Class
