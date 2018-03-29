# igicore - Queue [Standalone Edition]
Standalone queue system for FiveM servers.

This resource allows players to connect to your server while it's full and queue until there is an available slot.

# Config
Use the `igi_queue_disconnectGrace` to define the grace period (in seconds) for people who disconnect from the queue. This will allow them to reconnect within the grace period and retain their slot in the queue.

```
sv_maxclients = 32      # Default FiveM convar for max connected players

set igi_queue_disconnectGrace 60   # Disconnect grace period in seconds
```

# Development
Clone this repo inside your FiveM server's ``resources`` directory and build the project in Visual Studio 2017.

Edit your ``server.cfg`` file to include the following line below in your existing configuration:

```
start igicore-queue
```

