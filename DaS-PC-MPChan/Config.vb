Module Config
    Public Const VersionCheckUrl = "http://wulf2k.ca/pc/das/dscm-ver.txt"
    Public Const OnlineCheckUrl = "http://chronial.de/scripts/dscm/is_online.php"
    Public Const NetServerUrl = "http://dscm-net.chronial.de:8811"

    'How frequently should we update the node list from the central server?
    Public Const UpdateNetNodesInterval = 120 * 1000

    'How frequently should we publish our nodes to the central server?
    Public Const PublishNodesInterval = 60 * 1000

    'How frequently should we connect to node from the central server?
    Public Const NetNodeConnectInterval = 10 * 1000
    
    'How frequently should we check our watch node?
    Public Const CheckWatchNodeInterval = 20 * 1000
    'How frequently should we exchange our watch node?
    Public Const ExchangeWatchNodeInterval = 5 * 60 * 1000

    'How frequenlty should we check the only state of recent nodes and favourites via steam api?
    Public Const OnlineCheckInterval = 10 * 60 * 1000

    'For how long should a node be ignored for automatic connections after we tried connecting to them?
    Public Const ConnectionRetryTimeout = 5 * 60

    'Number of nodes we will try to leave free for steam matchmaking
    Public Const NodesReservedForSteam = 3

    'Number of free nodes we want to achieve by disconnecting
    Public Const DisconnectTargetFreeNodes = 5
    'Maximum number of bad nodes we tolerate
    Public Const BadNodesThreshold = 3

    'Grace period before a bad node may be disconnected
    Public Const BadNodeGracePeriod = 5 * 60
    'Grace period before a half-bad node may be disconnected
    Public Const HalfBadNodeGracePeriod = 15 * 60
    'Grace period for nodes that have been added manually
    Public Const ManualNodeGracePeriod = 15 * 60
End Module
