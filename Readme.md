Instructions
============

1. Start DSCM
2. Start Dark Souls
3. Enjoy Dark Souls Multiplayer :)

Automatic node finding
----------------------
While DSCM-Net is active, it will automatically connect to players in the same
area and in coop level range. Just have it running and enjoy working Dark Souls
multiplayer.

Manual Connecting
-----------------
You can also manually connect to a specific player like this:
1. Make sure you are connected to less than 20 nodes. You might want to disable
   DCSM-Net, so you get less nodes.
2. Copy the other player's steam profile URL or SteamID64 into the `Target` field.
3. Click `Attempt Connection`
4. After a few seconds, they should appear in the `Active` tab.
5. You might want to add the player to your favorites by selecting them in the
   Active list and clicking `Add Favorite`
6. Next time you can connect to the other player by double-clicking the
   favorites entry.


Troubleshooting
===============

* This program **does not work** with the beta version of Dark Souls. Opt out of
  the beta in steam settings:

	1. Right click on Dark Souls in your game library in steam
	2. Go to properties and locate the "betas" tab 
	3. Select an option that says "none - opt out of all betas"

* Make sure [normal in-game variables for multiplayer
  activity](http://darksouls.wikidot.com/co-op) are accounted for. Players
  commonly forget that:

    * The water must be drained in New Londo Ruins before the host can have any
      phantoms enter their world
    * Areas that still have yellow fog gates (the ones that get cleared after
      placing the Lord Vessel) don't allow multiplayer activity

* Is your game crashing when getting invaded or trying to use someone's summon sign? 

	* This is the namecrash bug, which is unrelated to this mod. 
	* Run PvP Watchdog, because it fixes this problem (read Named Nodes section below).

Features
========

Hotkeys:  
Ctrl + 1 = Toggle Node Drawing

Node Drawing
------------
Enabling this shows a visualization of currently active connections. 

* You are represented with the large circle in the middle. 
* Other players are smaller circles that are connected to you with a line.
* In-game proximity is represented here. If a player is near you in-game, like
  you're standing in the same spot in a level, your circles will be close to
  each other.
* The fact that the player names are not drawn near the nodes they belong to is
  a known issue.

DSCM-Net
--------
Due to constant problems with the default Steam matchmaking, DCSM contains its
own matchmaking. All DSCM-Net clients report player info to a central location.
You can have a look at this information in the DSCM-Net tab.

DSCM uses this information to automatically connect you to appropriate players.
Note that your covenant (and whether you are wearing its ring) will be taken
into account in this process.


Active Nodes Tab
----------------
This displays a detailed list of the players you're connected to. It also shows
their location in-game, and if they're human, hollow, or a phantom of some kind. 

This vastly helps with informing you if you have potential viable connections or
not. Bear in mind it is not possible to know whether or not someone has their
boss alive, so that aspect is still a guess from your end.

#### Row colors

The color of the row marks the usefulness that DSCM thinks that node has for
you. That is based on Location, Soul Level, your Covenant and if you have your
covenant ring equipped.

Nodes that you added manually are highlighted in bold.

#### MP Area column

This column shows the invasion zone the player is in. [The wiki](http://darksouls.wikidot.com/invasion-spawn-locations)
has further details.


Favorites and Recent Tabs
-------------------------
The recent tab will fill up with your 70 most recent connections. Favorites can
be added from either your active node tab or recent tab.
When starting DSCM and every 10 minutes after, DSCM will interact with a script
created and hosted by Chronial that checks the online status of your Recent and
favorite nodes (max 100 combined). Steam privacy settings can make a node appear
offline when they are not.


Disable FPS Disconnects
-----------------------

DSCM now disables Dark Souls' check that will kick you offline when your FPS dips
low.


Contact/ Bug Reports:
=====================

This program was created by Wulf, who can be found on reddit as
[/u/Wulf2k](https://www.reddit.com/u/wulf2k).
As a backup you can try contacting [/u/illusorywall](https://www.reddit.com/u/illusorywall)
or [@illusorywall](https://twitter.com/illusorywall), but I can't help as much
with the technical stuff.

Source code is available at: <https://github.com/Wulf2k/DaS-PC-MPChan>

- - - - - - - - - - - - - - - - - - - - - - - -
Brought to you by Lane Hatland (wulf2k@gmail.com).  
With thanks to Illusorywall, Jellybaby44, Chronial, and many others.
