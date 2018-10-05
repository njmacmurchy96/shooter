Module Raytracing
    Public Class Ray
        Public position As Vec2
        Public normal As Vec2
    End Class

    Public Function raytraceLine(p1 As Vec2, p2 As Vec2, rayNormal As Vec2, rayOrigin As Vec2) As Ray
        Dim output As New Ray
        Dim p3 As Vec2 = rayOrigin
        Dim p4 As Vec2 = rayOrigin + rayNormal
        Dim p As Single = (p1.x - p2.x) * (p3.y - p4.y) - (p1.y - p2.y) * (p3.x - p4.x)
        If Math.Abs(p) < 0.0001 Then 'parallel test
            Return Nothing
        End If
        output.position = New Vec2((((p1.x * p2.y) - (p1.y * p2.x)) * (p3.x - p4.x) - (p1.x - p2.x) * ((p3.x * p4.y) - (p3.y * p4.x))) / p,
                            (((p1.x * p2.y) - (p1.y * p2.x)) * (p3.y - p4.y) - (p1.y - p2.y) * ((p3.x * p4.y) - (p3.y * p4.x))) / p)

        If Vec2.dot(output.position - p1, output.position - p2) > 0 Or Vec2.dot(output.position - rayOrigin, rayNormal) < 0 Then
            Return Nothing
        End If

        output.normal = New Vec2(p1.y - p2.y, p2.x - p1.x).normalized

        Return output
    End Function

    Public Function raytraceCircle(center As Vec2, radius As Single, rayNormal As Vec2, rayOrigin As Vec2) As Ray
        Dim output As New Ray

        Dim t As Single = rayNormal.x * (center.x - rayOrigin.x) + rayNormal.y * (center.y - rayOrigin.y)
        Dim e As Vec2 = t * rayNormal + rayOrigin
        Dim de As Single = (e - center).length

        If de < radius Then
            Dim dt As Single = Math.Sqrt(radius * radius + de * de)
            output.position = (t - dt) * rayNormal + rayOrigin
            output.normal = (output.position - center).normalized
            Return output
        Else
            Return Nothing
        End If
    End Function

End Module
