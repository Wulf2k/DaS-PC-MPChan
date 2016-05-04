Module DSDataMaps
    Public PhantomType As New Dictionary(Of Integer, String)
    Public World As New Dictionary(Of String, String)
    Sub New()
        PhantomType.Add(-1, "Loading")
        PhantomType.Add(0, "Human")
        PhantomType.Add(1, "Co-op")
        PhantomType.Add(2, "Invader")
        PhantomType.Add(8, "Hollow")

        World.Add("-1--1", "None")
        World.Add("10-0", "Depths")
        World.Add("10-1", "Undead Burg/Parish")
        World.Add("10-2", "Firelink Shrine")
        World.Add("11-0", "Painted World")
        World.Add("12-0", "Darkroot Garden")
        World.Add("12-1", "Oolacile")
        World.Add("13-0", "Catacombs")
        World.Add("13-1", "Tomb of the Giants")
        World.Add("13-2", "Great Hollow / Ash Lake")
        World.Add("14-0", "Blighttown")
        World.Add("14-1", "Demon Ruins")
        World.Add("15-0", "Sen's Fortress")
        World.Add("15-1", "Anor Londo")
        World.Add("16-0", "New Londo Ruins")
        World.Add("17-0", "Duke's Archives / Caves")
        World.Add("18-0", "Kiln")
        World.Add("18-1", "Undead Asylum")
    End Sub
End Module



Public Class DSNode
    Public SteamId As String
    Public CharacterName As String
    Public SoulLevel As Integer
    Public PhantomType As Integer
    Public MPZone As Integer
    Public World As String

    Public Function MemberwiseEquals(other As DSNode) As Boolean
        If Object.ReferenceEquals(Me, other) Then Return True
        Return (
            SteamId = other.SteamId AndAlso
            CharacterName = other.CharacterName AndAlso
            SoulLevel = other.SoulLevel AndAlso
            PhantomType = other.PhantomType AndAlso
            MPZone = other.MPZone AndAlso
            World = other.World)
    End Function

    Public ReadOnly Property SteamIdColumn As String
        Get
            Return SteamId
        End Get
    End Property
        Public ReadOnly Property CharacterNameColumn As String
        Get
            Return CharacterName
        End Get
    End Property
        Public ReadOnly Property SoulLevelColumn As String
        Get
            Return SoulLevel
        End Get
    End Property
        Public ReadOnly Property MPZoneColumn As String
        Get
            Return MPZone
        End Get
    End Property
    Public ReadOnly Property PhantomTypeText As String
        Get
            Try
                Return DSDataMaps.PhantomType(PhantomType)
            Catch ex As KeyNotFoundException
                Return PhantomType.ToString()
            End Try
        End Get
    End Property
    Public ReadOnly Property WorldText As String
        Get
            Try
                Return DSDataMaps.World(World)
            Catch ex As KeyNotFoundException
                Return World
            End Try
        End Get
    End Property
    Public Function canCoop(other As DSNode) As Boolean
        Return Math.Abs(SoulLevel - other.SoulLevel) <= (10 + SoulLevel * 0.1)
    End Function
End Class
