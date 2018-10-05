Public Class Vec2
    Public x As Single
    Public y As Single


    Sub New()
        x = 0
        y = 0
    End Sub

    Sub New(xyValue As Single)
        x = xyValue
        y = xyValue
    End Sub

    Sub New(xValue As Single, yValue As Single)
        x = xValue
        y = yValue
    End Sub


    Public Shared Operator +(a As Vec2, b As Vec2) As Vec2
        Return New Vec2(a.x + b.x, a.y + b.y)
    End Operator

    Public Shared Operator -(a As Vec2, b As Vec2) As Vec2
        Return New Vec2(a.x - b.x, a.y - b.y)
    End Operator

    Public Shared Operator *(a As Vec2, b As Vec2) As Vec2
        Return New Vec2(a.x * b.x, a.y * b.y)
    End Operator

    Public Shared Operator /(a As Vec2, b As Vec2) As Vec2
        Return New Vec2(a.x / b.x, a.y / b.y)
    End Operator

    Public Shared Operator =(a As Vec2, b As Vec2) As Boolean
        Return a.x = b.x And a.y = b.y
    End Operator

    Public Shared Operator <>(a As Vec2, b As Vec2) As Boolean
        Return a.x <> b.x Or a.y <> b.y
    End Operator

    Public Shared Widening Operator CType(v As Single) As Vec2
        Return New Vec2(v)
    End Operator

    Public Shared Narrowing Operator CType(v As Vec2) As String
        Return "vec2(" & v.x & ", " & v.y & ")"
    End Operator


    Public Property length As Single
        Get
            Return Math.Sqrt(y * y + x * x)
        End Get
        Set(value As Single)
            Dim v As Vec2 = normalized * value
            x = v.x
            y = v.y
        End Set
    End Property

    Public Property normalized As Vec2
        Get
            Dim l As Single = length
            If l = 0 Then
                Return New Vec2(0, 1)
            End If
            Return Me / l
        End Get
        Set(value As Vec2)
            Dim v As Vec2 = value * length
            x = v.x
            y = v.y
        End Set
    End Property


    Public Shared Function dot(a As Vec2, b As Vec2) As Single
        Return a.x * b.x + a.y * b.y
    End Function

    Public Overloads Shared Function cross(a As Vec2, b As Vec2) As Single
        Return a.x * b.y - a.y * b.x
    End Function

    Public Overloads Shared Function cross(a As Single, b As Vec2) As Vec2
        Return New Vec2(-a * b.y, a * b.x)
    End Function

    Public Overloads Shared Function cross(a As Vec2, b As Single) As Vec2
        Return New Vec2(b * a.y, -b * a.x)
    End Function
End Class

