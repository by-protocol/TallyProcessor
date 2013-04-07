Class doubleKeyedDictionary(Of TKey1, TKey2, TValue)

    Private values As IDictionary
    Private keys1 As IDictionary
    Private keys2 As IDictionary
    Private id As Long

    Public Sub New()
        values = New Dictionary(Of Long, TValue)
        keys1 = New Dictionary(Of TKey1, Long)
        keys2 = New Dictionary(Of TKey2, Long)
        id = 0
    End Sub


    Public Function item(ByVal key1 As TKey1, ByVal key2 As TKey2) As TValue
        If containsKey1(key1) Then
            Return values.Item(keys1.Item(key1))
        ElseIf containsKey2(key2) Then
            Return values.Item(keys2.Item(key2))
        Else : Throw New KeyNotFoundException
        End If
    End Function

    Public Function getKey1(ByVal key2 As TKey2) As TKey1
        For Each key In keys1.Keys
            If keys1.Item(key).Equals(keys2.Item(key2)) Then
                Return key
            End If
        Next
        Throw New KeyNotFoundException
        'Return Nothing
    End Function

    Public Function getKey2(ByVal key1 As TKey1) As TKey2
        For Each key In keys2.Keys
            If keys2.Item(key).Equals(keys1.Item(key1)) Then
                Return key
            End If
        Next
        Throw New KeyNotFoundException
        'Return Nothing
    End Function

    Public Sub appendKey1(ByVal key1 As TKey1, ByVal key2 As TKey2)
        If containsKey2(key2) AndAlso Not IsNothing(key1) Then
            If Not containsKey1(key1) Then
                keys1.Add(key1, keys2.Item(key2))
            Else : Throw New DuplicateNameException
            End If
        Else : Throw New NoNullAllowedException
        End If
    End Sub

    Public Sub appendKey2(ByVal key2 As TKey2, ByVal key1 As TKey1)
        If containsKey1(key1) AndAlso Not IsNothing(key2) Then
            If Not containsKey2(key2) Then
                keys2.Add(key2, keys1.Item(key1))
            Else : Throw New DuplicateNameException
            End If
        Else : Throw New NoNullAllowedException
        End If
    End Sub

    Public Sub addByKey1(ByVal key1 As TKey1, ByRef value As TValue)
        Dim nid As Long = id + 1
        'If IsNothing(value) Then Throw New NoNullAllowedException
        If containsKey1(key1) Then
            Throw New DuplicateNameException
        ElseIf containsKey2ForValue(value) Then
            nid = getId(value)
            keys1.Add(key1, nid)
        Else
            keys1.Add(key1, nid)
            values.Add(nid, value)
            id = nid
        End If
    End Sub

    Public Sub addByKey2(ByVal key2 As TKey2, ByVal value As TValue)
        Dim nid As Long = id + 1
        'If IsNothing(value) Then Throw New NoNullAllowedException
        If containsKey2(key2) Then
            Throw New DuplicateNameException
        ElseIf containsKey1ForValue(value) Then
            nid = getId(value)
            keys2.Add(key2, nid)
        Else
            keys2.Add(key2, nid)
            values.Add(nid, value)
            id = nid
        End If
    End Sub

    Public Sub add(ByVal key1 As TKey1, ByVal key2 As TKey2, ByRef value As TValue)
        Dim nid As Long = id + 1
        'If IsNothing(value) Then Throw New NoNullAllowedException

        ' Wenn es schon einen key gibt aber nicht beide, prüfen ob update möglich (bei value neu = value gespeichert)
        If containsKey1(key1) Xor containsKey2(key2) Then
            If containsKey1(key1) AndAlso Not IsNothing(key2) Then
                nid = keys1.Item(key1)
                If values.Item(nid).Equals(value) Then
                    keys2.Add(key2, nid)
                Else : Throw New DuplicateNameException
                End If
            ElseIf containsKey2(key2) AndAlso Not IsNothing(key1) Then
                nid = keys2.Item(key2)
                If values.Item(nid).Equals(value) Then
                    keys1.Add(key1, nid)
                Else : Throw New DuplicateNameException
                End If
            Else : Throw New DuplicateNameException
            End If
        ElseIf Not (containsKey1(key1) AndAlso containsKey2(key2)) Then
            ' keiner der beiden keys ist schon gelistet, also hinzufügen
            If IsNothing(key1) Then
                Throw New NoNullAllowedException
            Else
                keys1.Add(key1, nid)
                id = nid
            End If
            If IsNothing(key2) Then
                Throw New NoNullAllowedException
            Else
                keys2.Add(key2, nid)
                id = nid
            End If
            If nid = id Then values.Add(nid, value)
        Else
            ' Beide keys sind schon bekannt, also ein Fehler
            Throw New DuplicateNameException
        End If

    End Sub

    Public Function remove(ByVal key1 As TKey1, ByVal key2 As TKey2) As TValue
        Dim vid As Long = -1
        Dim v As TValue = Nothing
        If keys1.Contains(key1) Then
            vid = keys1.Item(key1)
            keys1.Remove(key1)
        End If
        If keys2.Contains(key2) Then
            vid = keys2.Item(key2)
            keys2.Remove(key2)
        End If
        If vid >= 0 Then
            v = values.Item(vid)
            values.Remove(vid)
        End If
        Return v
    End Function

    Public Function containsValue(ByRef value As TValue) As Boolean
        For Each v As TValue In values.Values
            If v.Equals(value) Then
                Return True
            End If
        Next
        Return False
    End Function

    Public Function containsKey1(ByVal key1 As TKey1) As Boolean
        If IsNothing(key1) Then Return False
        Return keys1.Contains(key1)
    End Function

    Public Function containsKey2(ByVal key2 As TKey2) As Boolean
        If IsNothing(key2) Then Return False
        Return keys2.Contains(key2)
    End Function

    Public Function containsValueDoubleKeyed(ByRef value As TValue) As Boolean
        Return containsKey1ForValue(value) AndAlso containsKey2ForValue(value)
    End Function

    Private Function getId(ByRef value As TValue) As Long
        If containsValue(value) Then
            For Each vid In values.Keys
                If values.Item(vid).Equals(value) Then
                    Return vid
                End If
            Next
        End If
        Return -1
    End Function

    Public Function containsKey1ForValue(ByRef value As TValue) As Boolean
        Dim vid As Long = getId(value)
        If containsValue(value) Then
            For Each kid In keys1.Values
                If kid.Equals(vid) Then
                    Return True
                End If
            Next
        End If
        Return False
    End Function

    Public Function containsKey2ForValue(ByRef value As TValue) As Boolean
        Dim vid As Long = getId(value)
        If containsValue(value) Then
            For Each kid In keys2.Values
                If kid.Equals(vid) Then
                    Return True
                End If
            Next
        End If
        Return False
    End Function
End Class
