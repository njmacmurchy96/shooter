Public Class Form1
    Public debugDrawOptimization As Boolean = False 'ctrl + o
    Public debugDrawTransform As Boolean = False 'ctrl + t
    Public debugTestPlayerLock As Boolean = True 'ctrl + l

    Private sw As New Stopwatch
    Private deltaTime As Single
    Private lastTick As Long
    Private keyState(255) As Boolean 'true = down       false = up
    Private lMouseState As Boolean = False
    Private rand As New Random
    Private frameBuffer As Image
    Private frameBufferGraphics As Graphics

    Private cameraTransform As New TransformMatrix

    Private player As New Character
    Private playerShootSpeed As Single = 0.05F
    Private playerReloadTime As Single = 0F

    Private kills As Integer = 0

    Private barriers As New List(Of CollisionModel)
    Private particles As New List(Of Particle)
    Private deadParticles As New List(Of Particle)

    Private enemies As New List(Of Character)
    Private enemyClock As Single
    Private enemySpawnTime As Single
    Private enemySpawnSpeedMult As Single = 50
    Private enemyGrid(15, 7, 63) As Character 'enemy to enemy interaction optimization
    Private enemyGridScale As New Vec2(100, 100) 'size in pixels of grid spaces
    Private enemyGridDepth As Integer = 63 'max enemies per grid space
    Private enemyGridPosition As New Vec2 'grid's origin point

    Private temp As New CollisionModel With {.size = New Vec2(100, 80)}

    Private raySearchBlocks(15, 7) As Boolean

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        barriers.Add(temp)

        sw.Start()

        frameBuffer = New Bitmap(ClientSize.Width, ClientSize.Height)
        frameBufferGraphics = Graphics.FromImage(frameBuffer)

        player.position.x = ClientSize.Width * 0.5F
        player.position.y = ClientSize.Height * 0.5F
        cameraTransform.position = player.position

        player.size = 20
        For i As Integer = 1 To 50
            Dim p As New Vec2
            Dim b As New CollisionModel
            p.x = rand.Next(-1000, ClientSize.Width + 1000)
            p.y = rand.Next(-1000, ClientSize.Height + 1000)

            Dim f As New Vec2
            Dim a As Double = rand.NextDouble() * Math.PI * 2
            f.x = Math.Sin(a)
            f.y = Math.Cos(a)
            b.size.x = rand.Next(20, 200)
            b.size.y = rand.Next(20, 200)
            b.transform = New TransformMatrix(f, p)
            barriers.Add(b)
        Next

        temp.transform = New TransformMatrix(New Vec2(0, 1), New Vec2(400, 400))

    End Sub


    Private Sub GameClock_Tick(sender As Object, e As EventArgs) Handles GameClock.Tick

        deltaTime = (sw.ElapsedTicks - lastTick) / Stopwatch.Frequency
        lastTick = sw.ElapsedTicks

        enemyClock += deltaTime * enemySpawnSpeedMult
        If enemyClock >= enemySpawnTime Then
            enemySpawnTime = rand.Next(2, 10) '* 0.02
            enemyClock = 0

            Dim s As Integer = rand.Next(1, 5) '1 to 4
            Dim enemy As New Character
            enemy.size = player.size
            enemies.Add(enemy)

            If s = 1 Then
                enemy.position.x = 20 + player.position.x - ClientSize.Width * 0.5F
                enemy.position.y = rand.Next(20, ClientSize.Height + 20) + player.position.y - ClientSize.Height * 0.5F
            ElseIf s = 2 Then
                enemy.position.x = ClientSize.Width - 20 + player.position.x - ClientSize.Width * 0.5F
                enemy.position.y = rand.Next(20, ClientSize.Height - 20) + player.position.y - ClientSize.Height * 0.5F
            ElseIf s = 3 Then
                enemy.position.x = rand.Next(20, ClientSize.Width - 20) + player.position.x - ClientSize.Width * 0.5F
                enemy.position.y = ClientSize.Height - 20 + player.position.y - ClientSize.Height * 0.5F
            ElseIf s = 4 Then
                enemy.position.x = rand.Next(20, ClientSize.Width - 20) + player.position.x - ClientSize.Width * 0.5F
                enemy.position.y = 20 + player.position.y - ClientSize.Height * 0.5F
            End If
        End If





        Dim projectionTransform As New TransformMatrix(cameraTransform)
        projectionTransform.position -= New Vec2(ClientSize.Width * 0.5F, ClientSize.Height * 0.5F)
        Dim mp As Point = PointToClient(MousePosition)
        Dim v As New Vec2(mp.X, mp.Y)
        v = projectionTransform * v
        v -= temp.transform.position
        temp.transform.forward = v.normalized
        temp.transform.left = New Vec2(temp.transform.forward.y, -temp.transform.forward.x)



        player.velocity.x = (keyState(Keys.D) * -1 + keyState(Keys.A))
        player.velocity.y = (keyState(Keys.S) * -1 + keyState(Keys.W))
        If player.velocity.x <> 0 Or player.velocity.y <> 0 Then
            player.velocity = player.velocity.normalized * 150 * 2
        End If

        player.position += player.velocity * deltaTime
        checkCollision(player)

        createEnemyGrid()

        'For Each enemy As character In enemies
        '    enemy.velocity = (player.position - enemy.position).normalized * 200
        '    For Each enemy2 In enemies
        '        If Not Object.ReferenceEquals(enemy, enemy2) Then
        '            Dim d As Vec2 = (enemy2.position - enemy.position)
        '            enemy.velocity -= d.normalized * Math.Min(1, Math.Max(0, (50 - d.length) * 0.02)) * 200
        '        End If
        '    Next
        '    If enemy.velocity.length > 200 Then
        '        enemy.velocity.length = 200
        '    End If
        '    enemy.position += enemy.velocity * deltaTime
        '    checkCollision(enemy)
        'Next

        For Each enemy As Character In enemies
            For xOffset As Integer = -1 To 1
                For yOffset As Integer = -1 To 1
                    For z As Integer = 0 To enemyGrid.GetLength(2) - 1
                        If enemy.gridSpaceZ < 0 OrElse enemy.gridSpaceX + xOffset < 0 OrElse enemy.gridSpaceX + xOffset > enemyGrid.GetLength(0) - 1 OrElse enemy.gridSpaceY + yOffset < 0 OrElse enemy.gridSpaceY + yOffset > enemyGrid.GetLength(1) - 1 OrElse enemyGrid(enemy.gridSpaceX + xOffset, enemy.gridSpaceY + yOffset, z) Is Nothing Then
                            Exit For
                        End If
                        If Not Object.ReferenceEquals(enemy, enemyGrid(enemy.gridSpaceX + xOffset, enemy.gridSpaceY + yOffset, z)) Then
                            Dim d As Vec2 = (enemyGrid(enemy.gridSpaceX + xOffset, enemy.gridSpaceY + yOffset, z).position - enemy.position)
                            enemy.velocity -= d.normalized * Math.Min(1, Math.Max(0, (50 - d.length) * 0.02)) * 200
                            'enemyGrid(enemy.gridSpaceX + xOffset, enemy.gridSpaceY + yOffset, z).velocity += d.normalized * Math.Min(1, Math.Max(0, (50 - d.length) * 0.02)) * 200
                        End If
                    Next
                Next
            Next
        Next
        For Each enemy As Character In enemies
            enemy.velocity += (player.position - enemy.position).normalized * 200
            If enemy.velocity.length > 200 Then
                enemy.velocity.length = 200
            End If

            enemy.position += enemy.velocity * deltaTime
            checkCollision(enemy)
        Next


        If playerReloadTime < playerShootSpeed Then
            playerReloadTime += deltaTime
        End If
        If lMouseState = True Then
            shoot()
            playerReloadTime -= playerShootSpeed
        End If


        For Each p As Particle In particles
            p.age += deltaTime
            If p.age > p.lifetime Then
                deadParticles.Add(p)
            End If
        Next
        For Each p As Particle In deadParticles
            particles.Remove(p)
        Next
        deadParticles.Clear()

        If debugTestPlayerLock = True Then
            cameraTransform.position = player.position
        End If
        'Debug.Print((sw.ElapsedTicks - lastTick) / Stopwatch.Frequency)
        Refresh()
    End Sub


    Private Sub Form1_KeyDown(sender As Object, e As KeyEventArgs) Handles MyBase.KeyDown
        If e.KeyValue = Keys.O And keyState(Keys.ControlKey) Then
            debugDrawOptimization = Not debugDrawOptimization
        End If
        If e.KeyValue = Keys.T And keyState(Keys.ControlKey) Then
            debugDrawTransform = Not debugDrawTransform
        End If
        If e.KeyValue = Keys.L And keyState(Keys.ControlKey) Then
            debugTestPlayerLock = Not debugTestPlayerLock
        End If
        If e.KeyValue = Keys.E And keyState(Keys.ControlKey) Then
            Dim enemy As New Character
            enemy.position = player.position + New Vec2(0, 20)
            enemy.size = 20
            enemies.Add(enemy)
        End If
        If e.KeyValue = Keys.S And keyState(Keys.ControlKey) Then
            If enemySpawnSpeedMult = 0 Then
                enemySpawnSpeedMult = 50
            Else
                enemySpawnSpeedMult = 0
            End If
        End If

        If e.KeyValue <= 255 Then
            keyState(e.KeyValue) = True
        End If
    End Sub

    Private Sub Form1_KeyUp(sender As Object, e As KeyEventArgs) Handles MyBase.KeyUp
        If e.KeyValue <= 255 Then
            keyState(e.KeyValue) = False
        End If
    End Sub

    Private Sub checkCollision(ByRef p As Character)
        For Each c As CollisionModel In barriers
            Dim localP As Vec2 = c.transform.inverse * p.position
            'localP = p.position - c.transform.position
            'localP = New Vec2(Vec2.dot(localP, c.left), Vec2.dot(localP, c.forward))
            If localP.x + p.size * 0.5F > -c.size.x * 0.5F And localP.x - p.size * 0.5F < c.size.x * 0.5F And localP.y + p.size * 0.5F > -c.size.y * 0.5F And localP.y - p.size * 0.5F < c.size.y * 0.5F Then
                Dim contactNormal As New Vec2
                Dim contactPosition As New Vec2

                'corner coords
                Dim backRight As New Vec2(c.size.x * -0.5F, c.size.y * -0.5F)
                Dim backLeft As New Vec2(c.size.x * 0.5F, c.size.y * -0.5F)
                Dim frontRight As New Vec2(c.size.x * -0.5F, c.size.y * 0.5F)
                Dim frontLeft As New Vec2(c.size.x * 0.5F, c.size.y * 0.5F)

                'distance from corners
                Dim dBR As Single = (localP - backRight).length
                Dim dBL As Single = (localP - backLeft).length
                Dim dFR As Single = (localP - frontRight).length
                Dim dFL As Single = (localP - frontLeft).length

                If Math.Min(dBR, Math.Min(dBL, Math.Min(dFR, dFL))) < p.size * 0.5F Then
                    If dBR < dBL And dBR < dFR And dBR < dFL Then
                        contactPosition = backRight
                        contactNormal = (localP - backRight)
                        contactNormal.x = Math.Min(contactNormal.x, 0)
                        contactNormal.y = Math.Min(contactNormal.y, 0)
                        contactNormal = contactNormal.normalized
                    ElseIf dBL < dFR And dBL < dFL Then
                        contactPosition = backLeft
                        contactNormal = (localP - backLeft)
                        contactNormal.x = Math.Max(contactNormal.x, 0)
                        contactNormal.y = Math.Min(contactNormal.y, 0)
                        contactNormal = contactNormal.normalized
                    ElseIf dFR < dFL Then
                        contactPosition = frontRight
                        contactNormal = (localP - frontRight)
                        contactNormal.x = Math.Min(contactNormal.x, 0)
                        contactNormal.y = Math.Max(contactNormal.y, 0)
                        contactNormal = contactNormal.normalized
                    Else
                        contactPosition = frontLeft
                        contactNormal = (localP - frontLeft)
                        contactNormal.x = Math.Max(contactNormal.x, 0)
                        contactNormal.y = Math.Max(contactNormal.y, 0)
                        contactNormal = contactNormal.normalized
                    End If
                Else
                    'distance from sides
                    Dim dRight As Single = localP.x + p.size * 0.5F + c.size.x * 0.5F
                    Dim dLeft As Single = c.size.x * 0.5F - (localP.x - p.size * 0.5F)
                    Dim dBack As Single = localP.y + p.size * 0.5F + c.size.y * 0.5F
                    Dim dFront As Single = c.size.y * 0.5F - (localP.y - p.size * 0.5F)

                    If dRight < dLeft And dRight < dBack And dRight < dFront Then
                        'right
                        If Math.Abs(localP.y) < c.size.y * 0.5F Then
                            contactNormal.x = -1
                            contactPosition.x = c.size.x * -0.5F
                            contactPosition.y = localP.y
                        End If

                        'Debug.Print("dRight")
                    ElseIf dLeft < dBack And dLeft < dFront Then
                        'left
                        If Math.Abs(localP.y) < c.size.y * 0.5F Then
                            contactNormal.x = 1
                            contactPosition.x = c.size.x * 0.5F
                            contactPosition.y = localP.y
                        End If

                        'Debug.Print("dLeft")
                    ElseIf dBack < dFront Then
                        'back
                        If Math.Abs(localP.x) < c.size.x * 0.5F Then
                            contactNormal.y = -1
                            contactPosition.y = c.size.y * -0.5F
                            contactPosition.x = localP.x
                        End If

                        'Debug.Print("dBack")
                    Else
                        'front
                        If Math.Abs(localP.x) < c.size.x * 0.5F Then
                            contactNormal.y = 1
                            contactPosition.y = c.size.y * 0.5F
                            contactPosition.x = localP.x
                        End If

                        'Debug.Print("dFront")
                    End If
                End If

                If contactNormal.x <> 0 Or contactNormal.y <> 0 Then
                    'Debug.Print("valid contact")
                    contactNormal = c.transform.orientation * contactNormal
                    contactPosition = c.transform * contactPosition + contactNormal * p.size * 0.5F
                    p.position += contactNormal * ((p.position - contactPosition).length + 0.01F)
                End If
            End If
        Next
    End Sub

    Private Sub createEnemyGrid()
        If enemies.Count = 0 Then
            ReDim enemyGrid(1, 1, enemyGridDepth)
            Exit Sub
        End If

        Dim maxX As Single = 0
        Dim maxY As Single = 0
        Dim minX As Single = 99999
        Dim minY As Single = 99999

        For Each enemy As Character In enemies
            maxX = Math.Max(enemy.position.x, maxX)
            maxY = Math.Max(enemy.position.y, maxY)
            minX = Math.Min(enemy.position.x, minX)
            minY = Math.Min(enemy.position.y, minY)
        Next

        enemyGridPosition.x = minX - 15
        enemyGridPosition.y = minY - 15
        Dim sizeX As Integer = Math.Floor((maxX - minX + 30) / enemyGridScale.x)
        Dim sizeY As Integer = Math.Floor((maxY - minY + 30) / enemyGridScale.y)

        'if used to only update ray optimization grid on size changes to keep information for display
        If sizeX <> enemyGrid.GetLength(0) - 1 Or sizeY <> enemyGrid.GetLength(1) - 1 Then
            ReDim raySearchBlocks(sizeX, sizeY)
        End If

        ReDim enemyGrid(sizeX, sizeY, enemyGridDepth)


        For Each enemy As Character In enemies
            'If enemy.position.x > 0 And enemy.position.x < ClientSize.Width And enemy.position.y > 0 And enemy.position.y < ClientSize.Height Then
            Dim x As Integer = Math.Floor((enemy.position.x - enemyGridPosition.x) / enemyGridScale.x)
            Dim y As Integer = Math.Floor((enemy.position.y - enemyGridPosition.y) / enemyGridScale.y)
            For z As Integer = 0 To enemyGridDepth
                If enemyGrid(x, y, z) Is Nothing Then
                    enemyGrid(x, y, z) = enemy
                    enemy.gridSpaceX = x
                    enemy.gridSpaceY = y
                    enemy.gridSpaceZ = z
                    enemy.velocity.x = 0
                    enemy.velocity.y = 0
                    Exit For
                End If
            Next
            'End If
        Next
    End Sub

    Private Sub Form1_Paint(sender As Object, e As PaintEventArgs) Handles MyBase.Paint
        frameBufferGraphics.Clear(BackColor)

        Dim projectionTransform As New TransformMatrix(cameraTransform)
        projectionTransform.position -= New Vec2(ClientSize.Width * 0.5F, ClientSize.Height * 0.5F)
        Dim projectionTransformInverse As TransformMatrix = projectionTransform.inverse

        'for drawing non oriented shapes
        frameBufferGraphics.Transform = New Drawing2D.Matrix(projectionTransformInverse.elements(0, 0), projectionTransformInverse.elements(1, 0), projectionTransformInverse.elements(0, 1), projectionTransformInverse.elements(1, 1), projectionTransformInverse.elements(0, 2), projectionTransformInverse.elements(1, 2))

        'particle rendering
        For Each p As Particle In particles
            If p.type = Particle.ParticleRenderType.Line Then
                Dim lp As LineParticle = TryCast(p, Particle)
                If lp IsNot Nothing Then
                    Dim p1 As Vec2 = lp.p1
                    Dim p2 As Vec2 = lp.p2
                    frameBufferGraphics.DrawLine(New Pen(lp.color, lp.width), p1.x, p1.y, p2.x, p2.y)
                End If
            End If
        Next

        'show enemy optimization grid
        If debugDrawOptimization = True Then
            For x As Integer = 0 To enemyGrid.GetLength(0) '- 1
                Dim p1 As Vec2 = New Vec2(x * enemyGridScale.x + enemyGridPosition.x, player.position.y - ClientSize.Height * 0.5F)
                Dim p2 As Vec2 = New Vec2(x * enemyGridScale.x + enemyGridPosition.x, player.position.y + ClientSize.Height * 0.5F)
                frameBufferGraphics.DrawLine(Pens.LightGray, p1.x, p1.y, p2.x, p2.y)
            Next
            For y As Integer = 0 To enemyGrid.GetLength(1) '- 1
                Dim p1 As Vec2 = New Vec2(player.position.x - ClientSize.Width * 0.5F, y * enemyGridScale.y + enemyGridPosition.y)
                Dim p2 As Vec2 = New Vec2(player.position.x + ClientSize.Width * 0.5F, y * enemyGridScale.y + enemyGridPosition.y)
                frameBufferGraphics.DrawLine(Pens.LightGray, p1.x, p1.y, p2.x, p2.y)
            Next
            For x As Integer = 0 To enemyGrid.GetLength(0) - 1
                For y As Integer = 0 To enemyGrid.GetLength(1) - 1
                    Dim c As Integer = 0
                    For z As Integer = 0 To enemyGridDepth
                        If enemyGrid(x, y, z) IsNot Nothing Then
                            c += 1
                        End If
                    Next
                    If raySearchBlocks(x, y) Then
                        frameBufferGraphics.FillRectangle(New SolidBrush(Color.FromArgb(32, 255, 0, 0)), x * enemyGridScale.x + enemyGridPosition.x, y * enemyGridScale.y + enemyGridPosition.y, enemyGridScale.x, enemyGridScale.y)
                    End If
                    frameBufferGraphics.DrawString(c, New Font("Arial", 10), Brushes.DarkRed, x * enemyGridScale.x + enemyGridPosition.x + 8, y * enemyGridScale.y + enemyGridPosition.y + 8)
                Next
            Next
        End If



        'player
        Dim mp As Point = PointToClient(MousePosition)
        Dim lookVector As Vec2 = (projectionTransform * (New Vec2(mp.X, mp.Y)) - player.position).normalized
        frameBufferGraphics.FillEllipse(Brushes.Silver, player.position.x - player.size * 0.5F, player.position.y - player.size * 0.5F, player.size, player.size)
        frameBufferGraphics.DrawEllipse(Pens.Black, player.position.x - player.size * 0.5F, player.position.y - player.size * 0.5F, player.size, player.size)
        frameBufferGraphics.DrawLine(Pens.Black, player.position.x + lookVector.x * (player.size * 0.5F - 4), player.position.y + lookVector.y * (player.size * 0.5F - 4), player.position.x + lookVector.x * (player.size * 0.5F + 4), player.position.y + lookVector.y * (player.size * 0.5F + 4))

        For Each c As Character In enemies
            frameBufferGraphics.DrawEllipse(Pens.Black, c.position.x - c.size * 0.5F, c.position.y - c.size * 0.5F, c.size, c.size)
        Next

        For Each c As CollisionModel In barriers
            Dim finalTransform As TransformMatrix = projectionTransformInverse * c.transform
            If finalTransform.position.x > -c.size.x - c.size.y And finalTransform.position.x < ClientSize.Width + c.size.x + c.size.y And finalTransform.position.y > -c.size.x - c.size.y And finalTransform.position.y < ClientSize.Height + c.size.x + c.size.y Then
                frameBufferGraphics.Transform = New Drawing2D.Matrix(finalTransform.elements(1, 1), finalTransform.elements(1, 0), finalTransform.elements(0, 1), finalTransform.elements(0, 0), finalTransform.elements(0, 2), finalTransform.elements(1, 2))
                frameBufferGraphics.DrawRectangle(Pens.Black, -c.size.x * 0.5F, -c.size.y * 0.5F, c.size.x, c.size.y)

                'local vector visualization
                If debugDrawTransform = True Then
                    frameBufferGraphics.DrawEllipse(Pens.Black, c.size.x * 0.5F - 3, -3, 6, 6)
                    frameBufferGraphics.DrawRectangle(Pens.Black, -3, c.size.y * 0.5F - 3, 6, 6)
                End If
            End If
        Next


        'framerate and enemy stats
        frameBufferGraphics.ResetTransform()

        frameBufferGraphics.DrawString("FPS: " & (1.0F / deltaTime).ToString("N"), New Font("Arial", 10), Brushes.Black, 8, 8)
        frameBufferGraphics.DrawString("Enemies: " & (enemies.Count).ToString(), New Font("Arial", 10), Brushes.Black, 8, 26)
        frameBufferGraphics.DrawString("Kills: " & kills.ToString(), New Font("Arial", 10), Brushes.Black, 8, 44)


        e.Graphics.DrawImage(frameBuffer, 0, 0, ClientSize.Width, ClientSize.Height)
    End Sub

    Private Sub shoot()
        Dim mp As Point = PointToClient(MousePosition)

        Dim lookVector As Vec2 = cameraTransform * New Vec2(mp.X - ClientSize.Width * 0.5F, mp.Y - ClientSize.Height * 0.5F)
        lookVector = (lookVector - player.position).normalized
        'If debugTestPlayerLock = True Then
        '    lookVector = (New Vec2(mp.X - ClientSize.Width * 0.5F, mp.Y - ClientSize.Height * 0.5F)).normalized
        'Else
        '    lookVector = (New Vec2(mp.X, mp.Y) - player.position).normalized
        'End If

        Dim rayOrigin As Vec2 = player.position + lookVector * player.size * 0.5F
        lookVector.x += (rand.NextDouble() - 0.5F) * 0.25F
        lookVector.y += (rand.NextDouble() - 0.5F) * 0.25F
        lookVector = lookVector.normalized
        Dim rayHits As New List(Of Ray)
        Dim closestHit As Ray = Nothing
        For Each c As CollisionModel In barriers
            collectRayHits(rayHits, lookVector, rayOrigin)
        Next
        For Each hit As Ray In rayHits
            If closestHit Is Nothing OrElse (closestHit.position - rayOrigin).length > (hit.position - rayOrigin).length Then
                closestHit = hit
            End If
        Next

        If closestHit Is Nothing Then
            closestHit = New Ray
            closestHit.position = rayOrigin + lookVector * 2000
        End If

        'enemy raytracing optimization 
        ReDim raySearchBlocks(enemyGrid.GetLength(0) - 1, enemyGrid.GetLength(1) - 1)
        Dim xBoundMin As Integer = Math.Min(Math.Floor((closestHit.position.x - enemyGridPosition.x) / enemyGridScale.x), Math.Floor((rayOrigin.x - enemyGridPosition.x) / enemyGridScale.x))
        Dim xBoundMax As Integer = Math.Max(Math.Ceiling((closestHit.position.x - enemyGridPosition.x) / enemyGridScale.x), Math.Ceiling((rayOrigin.x - enemyGridPosition.x) / enemyGridScale.x))
        Dim localOrigin As Vec2 = (rayOrigin - enemyGridPosition) / enemyGridScale
        Dim d As Vec2 = lookVector / enemyGridScale
        d = d / d.x
        Dim yOffset As Single = localOrigin.y - d.y * localOrigin.x

        For x As Integer = Math.Max(0, xBoundMin) To Math.Min(enemyGrid.GetLength(0) - 1, xBoundMax)
            Dim y0 As Single = x * d.y + yOffset - 0.5
            Dim y1 As Single = (x + 1) * d.y + yOffset - 0.5
            For y As Integer = Math.Max(0, Math.Min(y0, y1)) To Math.Min(enemyGrid.GetLength(1) - 1, Math.Max(y0, y1))
                raySearchBlocks(x, y) = True
            Next
        Next

        Dim raySearchBlocksExpanded(enemyGrid.GetLength(0) - 1, enemyGrid.GetLength(1) - 1) As Boolean
        For x As Integer = 0 To raySearchBlocks.GetLength(0) - 1
            For y As Integer = 0 To raySearchBlocks.GetLength(1) - 1
                raySearchBlocksExpanded(x, y) = raySearchBlocks(Math.Max(x - 1, 0), y) Or raySearchBlocks(x, y) Or raySearchBlocks(Math.Min(x + 1, raySearchBlocks.GetLength(0) - 1), y)
            Next
        Next
        For x As Integer = 0 To raySearchBlocks.GetLength(0) - 1
            For y As Integer = 0 To raySearchBlocks.GetLength(1) - 1
                raySearchBlocks(x, y) = raySearchBlocksExpanded(x, Math.Max(y - 1, 0)) Or raySearchBlocksExpanded(x, y) Or raySearchBlocksExpanded(x, Math.Min(y + 1, raySearchBlocksExpanded.GetLength(1) - 1))
            Next
        Next

        'enemy raytracing
        Dim enemyHit As Character = Nothing
        For x As Integer = 0 To enemyGrid.GetLength(0) - 1
            For y As Integer = 0 To enemyGrid.GetLength(1) - 1
                If raySearchBlocks(x, y) = True Then
                    For z As Integer = 0 To enemyGridDepth
                        If enemyGrid(x, y, z) IsNot Nothing Then
                            Dim hit As Ray = raytraceCircle(enemyGrid(x, y, z).position, enemyGrid(x, y, z).size * 0.5F, lookVector, rayOrigin)
                            If hit IsNot Nothing Then
                                If Vec2.dot(hit.position - rayOrigin, lookVector) > 0 Then
                                    If closestHit Is Nothing OrElse (closestHit.position - rayOrigin).length > (hit.position - rayOrigin).length Then
                                        closestHit = hit
                                        enemyHit = enemyGrid(x, y, z)
                                    End If
                                End If
                            End If
                        Else
                            Exit For
                        End If
                    Next
                End If
            Next
        Next
        If enemyHit IsNot Nothing Then
            If enemies.Remove(enemyHit) Then
                kills += 1
            End If
        End If

        Dim p As New LineParticle
        p.p1 = rayOrigin
        p.p2 = closestHit.position
        p.color = Color.Orange
        p.width = 1
        p.lifetime = 0.1
        particles.Add(p)
        'Debug.Print(lookVector)
        'Debug.Print(p.p2)
    End Sub

    Private Sub Form1_MouseDown(sender As Object, e As MouseEventArgs) Handles MyBase.MouseDown
        If e.Button = MouseButtons.Left Then
            lMouseState = True
        End If
    End Sub

    Private Sub Form1_MouseUp(sender As Object, e As MouseEventArgs) Handles MyBase.MouseUp
        If e.Button = MouseButtons.Left Then
            lMouseState = False
        End If
    End Sub

    Private Sub collectRayHits(ByRef rayHits As List(Of Ray), rayNormal As Vec2, rayOrigin As Vec2)
        For Each c As CollisionModel In barriers
            Dim tr As Vec2 = c.transform.position + c.transform.left * c.size.x * 0.5F + c.transform.forward * c.size.y * 0.5F
            Dim tl As Vec2 = c.transform.position - c.transform.left * c.size.x * 0.5F + c.transform.forward * c.size.y * 0.5F
            Dim bl As Vec2 = c.transform.position - c.transform.left * c.size.x * 0.5F - c.transform.forward * c.size.y * 0.5F
            Dim br As Vec2 = c.transform.position + c.transform.left * c.size.x * 0.5F - c.transform.forward * c.size.y * 0.5F


            Dim trtl As Ray = raytraceLine(tr, tl, rayNormal, rayOrigin)
            If trtl IsNot Nothing Then rayHits.Add(trtl)

            Dim tlbl As Ray = raytraceLine(tl, bl, rayNormal, rayOrigin)
            If tlbl IsNot Nothing Then rayHits.Add(tlbl)

            Dim blbr As Ray = raytraceLine(br, bl, rayNormal, rayOrigin)
            If blbr IsNot Nothing Then rayHits.Add(blbr)

            Dim brtr As Ray = raytraceLine(br, tr, rayNormal, rayOrigin)
            If brtr IsNot Nothing Then rayHits.Add(brtr)
        Next
    End Sub

    Private Overloads Function clamp(x As Integer, min As Integer, max As Integer) As Integer
        Return Math.Max(min, Math.Min(max, x))
    End Function

    Private Overloads Function clamp(x As Single, min As Single, max As Single) As Single
        Return Math.Max(min, Math.Min(max, x))
    End Function

    Private Sub Form1_Resize(sender As Object, e As EventArgs) Handles MyBase.Resize
        frameBuffer = New Bitmap(ClientSize.Width, ClientSize.Height)
        frameBufferGraphics = Graphics.FromImage(frameBuffer)
    End Sub

End Class



Public Class Character
    Public position As New Vec2
    Public size As Single
    Public velocity As New Vec2

    Public gridSpaceX As Integer = -1 'for enemies
    Public gridSpaceY As Integer = -1
    Public gridSpaceZ As Integer = -1
End Class



Public Class Particle
    Public type As Integer = -1
    Public lifetime As Single
    Public age As Single

    Public Enum ParticleRenderType
        Line
        Circle
    End Enum
End Class

Public Class LineParticle
    Inherits Particle
    Public p1 As Vec2
    Public p2 As Vec2
    Public color As Color
    Public width As Single = 1

    Sub New()
        type = ParticleRenderType.Line
    End Sub
End Class


Public Class CollisionModel
    Public transform As New TransformMatrix 'y is forward
    Public size As New Vec2
End Class