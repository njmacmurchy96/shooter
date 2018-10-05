Public Class TransformMatrix ' row, column   uses column vectors
    Public elements(,) As Single = {{1, 0, 0},
                                    {0, 1, 0},
                                    {0, 0, 1}}


    Sub New()

    End Sub

    Sub New(mx As TransformMatrix)
        For row As Integer = 0 To 2
            For column As Integer = 0 To 2
                elements(row, column) = mx.elements(row, column)
            Next
        Next
    End Sub

    Sub New(left As Vec2, forward As Vec2, position As Vec2)
        elements(0, 0) = left.x
        elements(1, 0) = left.y
        elements(0, 1) = forward.x
        elements(1, 1) = forward.y
        elements(0, 2) = position.x
        elements(1, 2) = position.y
    End Sub

    Sub New(forward As Vec2, position As Vec2)
        elements(0, 0) = forward.y
        elements(1, 0) = -forward.x
        elements(0, 1) = forward.x
        elements(1, 1) = forward.y
        elements(0, 2) = position.x
        elements(1, 2) = position.y
    End Sub

    Sub New(n As Single)
        For x As Integer = 0 To 2
            For y As Integer = 0 To 2
                elements(x, y) = n
            Next
        Next
    End Sub

    Public Property forward As Vec2
        Get
            Return New Vec2(elements(0, 1), elements(1, 1))
        End Get
        Set(value As Vec2)
            elements(0, 1) = value.x
            elements(1, 1) = value.y
        End Set
    End Property

    Public Property left As Vec2
        Get
            Return New Vec2(elements(0, 0), elements(1, 0))
        End Get
        Set(value As Vec2)
            elements(0, 0) = value.x
            elements(1, 0) = value.y
        End Set
    End Property

    Public Property position As Vec2
        Get
            Return New Vec2(elements(0, 2), elements(1, 2))
        End Get
        Set(value As Vec2)
            elements(0, 2) = value.x
            elements(1, 2) = value.y
        End Set
    End Property

    Public Property orientation As TransformMatrix
        Get
            Dim output As New TransformMatrix
            For row As Integer = 0 To 2
                For column As Integer = 0 To 1
                    output.elements(row, column) = elements(row, column)
                Next
            Next
            Return output
        End Get
        Set(value As TransformMatrix)
            For row As Integer = 0 To 2
                For column As Integer = 0 To 1
                    elements(row, column) = value.elements(row, column)
                Next
            Next
        End Set
    End Property

    Public ReadOnly Property determinant As Single
        Get
            Return elements(0, 0) * elements(1, 1) * elements(2, 2) + elements(0, 2) * elements(1, 2) * elements(2, 0) + elements(0, 2) * elements(1, 0) * elements(2, 1) -
                elements(2, 0) * elements(1, 1) * elements(0, 2) - elements(2, 1) * elements(1, 2) * elements(0, 0) - elements(2, 2) * elements(1, 0) * elements(0, 1)
        End Get
    End Property

    Public ReadOnly Property inverse As TransformMatrix
        Get
            If determinant <> 0 Then
                Dim output As New TransformMatrix
                output.elements(0, 0) = (elements(1, 1) * elements(2, 2) - elements(1, 2) * elements(2, 1)) / determinant
                output.elements(1, 0) = (elements(1, 0) * elements(2, 2) - elements(1, 2) * elements(2, 0)) / -determinant
                output.elements(2, 0) = (elements(1, 0) * elements(2, 1) - elements(1, 1) * elements(2, 0)) / determinant

                output.elements(0, 1) = (elements(0, 1) * elements(2, 2) - elements(0, 2) * elements(2, 1)) / -determinant
                output.elements(1, 1) = (elements(0, 0) * elements(2, 2) - elements(0, 2) * elements(2, 0)) / determinant
                output.elements(2, 1) = (elements(0, 0) * elements(2, 1) - elements(0, 1) * elements(2, 0)) / -determinant

                output.elements(0, 2) = (elements(0, 1) * elements(1, 2) - elements(0, 2) * elements(1, 1)) / determinant
                output.elements(1, 2) = (elements(0, 0) * elements(1, 2) - elements(0, 2) * elements(1, 0)) / -determinant
                output.elements(2, 2) = (elements(0, 0) * elements(1, 1) - elements(0, 1) * elements(1, 0)) / determinant
                'For row As Integer = 0 To 2
                '    For column As Integer = 0 To 2
                '        output.elements(row, column) = elements(row, column) / determinant
                '    Next
                'Next
                Return output
            Else
                Return Nothing
            End If
        End Get
    End Property


    Public Shared Operator *(a As TransformMatrix, b As TransformMatrix) As TransformMatrix
        Dim output As New TransformMatrix
        For row As Integer = 0 To 2
            For column As Integer = 0 To 2
                output.elements(row, column) = a.elements(row, 0) * b.elements(0, column) + a.elements(row, 1) * b.elements(1, column) + a.elements(row, 2) * b.elements(2, column)
            Next
        Next
        Return output
    End Operator

    Public Shared Operator *(a As TransformMatrix, b As Vec2) As Vec2
        Dim output As New Vec2
        output.x = b.x * a.elements(0, 0) + b.y * a.elements(0, 1) + a.elements(0, 2)
        output.y = b.x * a.elements(1, 0) + b.y * a.elements(1, 1) + a.elements(1, 2)
        Return output
    End Operator


End Class
