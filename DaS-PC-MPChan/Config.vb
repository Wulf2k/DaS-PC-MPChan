Module Config
    'How frequently should we connect to node from IRC?
    Public Const IRCNodeConnectInterval = 10 * 1000

    'How frequenlty should we check the only state of recent nodes and favourites via steam api?
    Public Const OnlineCheckInterval = 10 * 60 * 1000

    'For how long should a node be ignored for automatic connections after we tried connecting to them?
    Public Const ConnectionRetryTimeout = 5 * 60

    'Number of nodes we will try to leave free for steam matchmaking
    Public Const NodesReservedForSteam = 3

    'Number of free nodes we want to achieve by disconnecting
    Public Const DisconnectTargetFreeNodes = 5

    'Grace period before a bad node may be disconnected
    Public Const BadNodeGracePeriod = 5 * 60
    'Grace period before a half-bad node may be disconnected
    Public Const HalfBadNodeGracePeriod = 15 * 60
    'Grace period for nodes that have been added manually
    Public Const ManualNodeGracePeriod = 15 * 60


    Public Const IRCHost = "dscm.wulf2k.ca"
    Public Const IRCPort = 8123
    Public Const IRCOwner = "DSCMbot"
    Public Const IRCChannel = "#DSCM-Main"
    Public Const IRCNick = "DSCM"

    Public Const IRCPublishInterval = 120

    'TTL of known IRC nodes. Should be at least PublishInterval + ???
    Public Const IRCNodeTTL = 5 * 60

    'Minimum time to wait before we publish a node published by somebody else IF we have no new information
    Public Const IRCNodePublishInterval = 3 * 60
    'Minimum time to wait before we publish a node published by somebody else IF we have new information
    Public Const IRCNodeUpdateInterval = 20
End Module
