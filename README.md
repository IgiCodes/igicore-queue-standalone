# igicore - Queue [Standalone Edition]
Standalone queue system for FiveM servers.

This resource allows players to connect to your server while it's full and queue until there is an available slot.

[![License](https://img.shields.io/github/license/Igirisujin/igicore-queue-standalone.svg)](LICENSE)
[![Release Version](https://img.shields.io/github/release/Igirisujin/igicore-queue-standalone.svg)](https://github.com/Igirisujin/igicore-queue-standalone/releases)

# Download and Installation
Downloads can be found on the [Releases](https://github.com/Igirisujin/igicore-queue-standalone/releases/latest) page.
Follow the instructions on the release on how to install.

# Config
Use the `igi_queue_config` to define the path to the queue settings YAML file. This can be a full path or the path relative to where you launch the server from (e.g. where your startserver.sh is).

```lua
sv_maxclients = 32      # Default FiveM convar for max connected players

set igi_config_path "resources/[IgiCore]/igicore-queue/queueSettings.yml"  # Path to the config file relative to where you launch the server
```

Remove the default fivem `hardcap` resource from your server.cfg. This resource replaces it's functionality and it may interfere.
```lua
start hardcap   # remove this from your server.cfg if it's there
```

## YAML Config File
The release zip contains an example for the queueSettings.yml file. This file allows you to specify the disconnect grace period and player priority for players on your server.

`PriorityPlayers` is a list of players with a custom priority value (Anyone not in this list will default to Priority = 100)
`DisconnectGrace` is how long in seconds a player can disconnect from the queue and keep their spot until they reconnect (Default: 60).
`MaxClients` is also an extra value you can specify to override what the queue thinks the server's max client limit is (Default: this will be read from the sv_maxclients convar in your server.cfg). Useful for debugging purposes.
`QueueWhenNotFull` will determine whether or not to queue players even when the server isn't full; essentially only letting one player in at a time (Default: false).
`ConnectingTimeout` is how long in seconds a player can take to connect to the server from the queue (Default 120). Sometimes clients get stuck "connecting" and this causes everyone else to get stuck in the queue until manually reset.

Example file:
```yml
PriorityPlayers:  # List of players with a custom priority value (Anyone not in this list will default to Priority = 100)
    - SteamId:  1100001056a8b25 # Igi
      Priority: 10  # Lower number == higher priority

    - SteamId:  1100001056a8b13
      Priority: 50

    # ... More players here

DisconnectGrace: 60   # In Seconds
QueueWhenNotFull: false   # true/false
ConnectingTimeout: 120   # In Seconds
```

# Commands
There are few commands available to you via RCON (or the server terminal), all prefixed with the `queue` commands.

`queue help` - Output the all possible commands in the console.

`queue status` - Output the current queue.

`queue clear` - Clear the current queue (people will have to reconnect).

`queue reload` - Reload the queue settings file.

`queue add <steamid>` - Manually insert a steamid into the queue (useful for debugging purposes).

`queue remove <steamid>` - Remove a specific steamid from the queue.

`queue move <steamid> [position]` - Move a specific steamid to a position in the queue (defaults to 1st in queue if not passed a position).


# Development
Clone this repo inside your FiveM server's ``resources`` directory and build the project in Visual Studio 2017.

Edit your ``server.cfg`` file to include the following line below in your existing configuration:

```lua
start igicore-queue
```

