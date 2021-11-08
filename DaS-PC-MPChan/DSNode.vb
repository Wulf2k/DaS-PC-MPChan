Module DSDataMaps
    Public PhantomType As New Dictionary(Of Integer, String)
    Public World As New Dictionary(Of String, String)
    Public Covenant As New Dictionary(Of Integer, String)
    Public DarkrootGardenZones As New HashSet(Of Integer)({120100, 120101})
    Public AnorLondoWorld = "15-1"

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
        World.Add("10-3", "Arena+")

        Covenant.Add(-1, "")
        Covenant.Add(0, "None")
        Covenant.Add(1, "Way of White")
        Covenant.Add(2, "Princess's Guard")
        Covenant.Add(3, "Warrior of Sunlight")
        Covenant.Add(4, "Darkwraith")
        Covenant.Add(5, "Path of the Dragon")
        Covenant.Add(6, "Gravelord Servant")
        Covenant.Add(7, "Forest Hunter")
        Covenant.Add(8, "Darkmoon Blade")
        Covenant.Add(9, "Chaos Servant")
    End Sub
End Module

Enum Covenant
    None = 0
    WayOfWhite = 1
    PrincessGuard = 2
    WarriorOfSunlight = 3
    Darkwraith = 4
    PathOfTheDragon = 5
    GravelordServant = 6
    ForestHunter = 7
    DarkmoonBlade = 8
    ChaosServant = 9
End Enum

Enum PhantomType
    Loading = -1
    Human = 0
    Coop = 1
    Invader = 2
    Hollow = 8
End Enum

Public Class DSNode
    Public SteamId As String
    Public CharacterName As String
    Public SoulLevel As Integer
    Public PhantomType As Integer
    Public MPZone As Integer
    Public World As String
    Public Covenant As Integer = -1
    Public Indictments As Integer = -1
    Public Ping As Integer = -1


    Public Function Clone() As DSNode
        Return DirectCast(Me.MemberwiseClone(), DSNode)
    End Function
    Public Function MemberwiseEquals(other As DSNode) As Boolean
        If Object.ReferenceEquals(Me, other) Then Return True
        Return (
            SteamId = other.SteamId AndAlso
            CharacterName = other.CharacterName AndAlso
            SoulLevel = other.SoulLevel AndAlso
            PhantomType = other.PhantomType AndAlso
            MPZone = other.MPZone AndAlso
            World = other.World AndAlso
            Covenant = other.Covenant AndAlso
            Indictments = other.Indictments)
    End Function
    Public Function BasicEquals(other As DSNode) As Boolean
        If Object.ReferenceEquals(Me, other) Then Return True
        Return (
            SteamId = other.SteamId AndAlso
            CharacterName = other.CharacterName AndAlso
            SoulLevel = other.SoulLevel AndAlso
            PhantomType = other.PhantomType AndAlso
            MPZone = other.MPZone AndAlso
            World = other.World)
    End Function
    Public ReadOnly Property HasExtendedInfo As Boolean
        Get
            Return Covenant <> -1
        End Get
    End Property
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
            Return If(SoulLevel >= 0, SoulLevel, "")
        End Get
    End Property
    Public ReadOnly Property SoulLevelColumnSort As Integer
        Get
            Return SoulLevel
        End Get
    End Property
    Public ReadOnly Property PingColumn As String
        Get
            Return If(Ping <= 0, "", Ping)
        End Get
    End Property
    Public ReadOnly Property MPZoneColumn As String
        Get
            If (MPZone = 103000) Then                 ' I have no idea how to write a switch in this godforsaken language 
                Return "Lobby:Bloodgulch"
            ElseIf (MPZone = 103010) Then
                Return "Bloodgulch"
            ElseIf (MPZone = 103020) Then
                Return "Lobby:Hang'Em:CTL"
            ElseIf (MPZone = 103030) Then
                Return "Hang'Em:CTL"
            ElseIf (MPZone = 103040) Then
                Return "Lobby:Hang'Em:TDM"
            ElseIf (MPZone = 103050) Then
                Return "Hang'Em:TDM"
            ElseIf (MPZone = 103060) Then
                Return "Lobby:Damnation"
            ElseIf (MPZone = 103070) Then
                Return "Damnation"
            ElseIf (MPZone = 103080) Then
                Return "Lobby:Sanctuary:TDM"
            ElseIf (MPZone = 103090) Then
                Return "Sanctuary : TDM"
            ElseIf (MPZone = 103100) Then
                Return "Lobby:Sanctuary:CTL"
            ElseIf (MPZone = 103110) Then
                Return "Sanctuary:CTL"
            ElseIf (MPZone = 103120) Then
                Return "Lobby:Lockout"
            ElseIf (MPZone = 103130) Then
                Return "Lockout"
            ElseIf (MPZone = 103140) Then
                Return "Lobby:Wizard"
            ElseIf (MPZone = 103150) Then
                Return "Wizard"
            ElseIf (MPZone = 103160) Then
                Return "Lobby:Prisoner"
            ElseIf (MPZone = 103170) Then
                Return "Prisoner"
            ElseIf (MPZone = 103180) Then
                Return "Lobby:Chillout"
            ElseIf (MPZone = 103190) Then
                Return "Chillout"
            ElseIf (MPZone = 103200) Then
                Return "Lobby:Ivory Tower"
            ElseIf (MPZone = 103210) Then
                Return "Ivory Tower"
            ElseIf (MPZone = 103220) Then
                Return "Lobby:Death Island"
            ElseIf (MPZone = 103230) Then
                Return "Death Island"
            ElseIf (MPZone = 103240) Then
                Return "Lobby:Crossfire"
            ElseIf (MPZone = 103250) Then
                Return "Crossfire"
            End If
            Return If(MPZone <= 0, "", MPZone)
        End Get
    End Property
    Public ReadOnly Property MPZoneColumnSort As Integer
        Get
            Return If(MPZone <= 0, 0, MPZone)
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
    Public ReadOnly Property CovenantColumn As String
        Get
            Try
                Return DSDataMaps.Covenant(Covenant)
            Catch ex As KeyNotFoundException
                Return Covenant.ToString()
            End Try
        End Get
    End Property
    Public ReadOnly Property IndictmentsColumn As String
        Get
            If Indictments = -1 Then
                Return ""
            Else
                Return Indictments
            End If
        End Get
    End Property
    Public ReadOnly Property IndictmentsColumnSort As Integer
        Get
            Return Indictments
        End Get
    End Property

    Public Function canBeSummoned(other As DSNode) As Boolean
        'This is also canGravelord and canBeDragonSummoned
        Return Math.Abs(SoulLevel - other.SoulLevel) <= (10 + SoulLevel * 0.1)
    End Function
    Public Function canForestInvade(other As DSNode) As Boolean
        Return SoulLevel - (10 + SoulLevel * 0.1) < other.SoulLevel
    End Function
    Public Function canBeRedSignSummoned(other As DSNode) As Boolean
        Return canForestInvade(other)
    End Function
    Public Function canRedEyeInvade(other As DSNode) As Boolean
        Return SoulLevel - (SoulLevel * 0.1) < other.SoulLevel
    End Function
    Public Function canDarkmoonInvade(other As DSNode) As Boolean
        Return (SoulLevel - (50 + SoulLevel * 0.2) < other.SoulLevel And
                other.SoulLevel < SoulLevel + (10 + SoulLevel * 0.1))
    End Function
End Class