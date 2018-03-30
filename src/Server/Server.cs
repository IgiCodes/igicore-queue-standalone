using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using IgiCore_Queue.Server.Models;
using Debug = CitizenFX.Core.Debug;

namespace IgiCore_Queue.Server
{
    public class Server : BaseScript
    {
        private readonly List<QueuePlayer> _queue = new List<QueuePlayer>();
        private static Config _config;
        private static string _configPath;

        public Server()
        {
            try
            {
                _configPath = API.GetConvar("igi_queue_config", "queueSettings.yml");
                _config = Config.Load(_configPath);

                DebugLog($"Max Clients: {_config.MaxClients}");
                DebugLog($"Disconnect Grace: {_config.DisconnectGrace}");
                HandleEvent<Player, string, CallbackDelegate, ExpandoObject>("playerConnecting", OnPlayerConnecting);
                HandleEvent<Player, string, CallbackDelegate>("playerDropped", OnPlayerDropped);
                HandleEvent<Player>("igicore:queue:playerActive", OnPlayerActive);
                HandleEvent<string, List<object>>("rconCommand", OnRconCommand);

                Tick += ProcessQueue;
            }
            catch (Exception e)
            {
                Log(e.Message);
                throw;
            }
            
        }

        private void OnRconCommand(string command, List<object> objargs)
        {
            if (command.ToLowerInvariant() != "queue") return;
            List<string> args = objargs.Cast<string>().ToList();

            switch (args[0].ToLowerInvariant())
            {
                case "reload":
                    string initServerName = _config.ServerName;
                    _config = Config.Load(_configPath);
                    _config.ServerName = initServerName;
                    break;
                case "clear":
                    _queue.Clear();
                    Log("Queue cleared!");
                    break;
                case "add":
                    if (args.Count < 2)
                    {
                        Log("Please pass a steam ID to add to the queue");
                        return;
                    }
                    // Check if the player is in the priority list
                    PriorityPlayer priorityPlayer = _config.PriorityPlayers.FirstOrDefault(p => p.SteamId == args[1]);
                    // Add to queue
                    AddToQueue(new QueuePlayer
                        {
                            SteamId = args[1],
                            Name = $"Manual Player - {args[1]}",
                            ConnectCount = 1,
                            ConnectTime = DateTime.UtcNow,
                            DisconnectTime = DateTime.UtcNow,
                            Status = QueueStatus.Disconnected,
                            Priority = priorityPlayer?.Priority ?? 100
                        }
                    );
                    break;
                case "status":
                    Log("Queue:");
                    foreach (QueuePlayer queuePlayer in _queue)
                    {
                        Log($"{_queue.IndexOf(queuePlayer) + 1}: " +
                            $"{queuePlayer.Name} - {queuePlayer.SteamId} " +
                            $"[Priority: {queuePlayer.Priority}] " +
                            $"[Status: {Enum.GetName(typeof(QueueStatus), queuePlayer.Status)}] " +
                            $"[Connected: {queuePlayer.ConnectTime.ToLocalTime()}]");
                    }
                    break;
                case "move":
                    if (args.Count < 2)
                    {
                        Log("Please pass a steam ID to move");
                        return;
                    }

                    QueuePlayer playerInQueue = _queue.FirstOrDefault(p => p.SteamId == args[1]);
                    if (playerInQueue == null)
                    {
                        Log("Player not found in queue");
                        return;
                    }

                    if (args.Count < 3) args.Add("1"); // Default to first in queue

                    _queue.Remove(playerInQueue);
                    _queue.Insert(int.Parse(args[2]) - 1, playerInQueue);

                    Log($"Moved player {playerInQueue.Name} ({playerInQueue.SteamId}) to position {args[2]}");

                    break;
                default:
                    Log("No such command exists");
                    break;
            }

            Function.Call(Hash.CANCEL_EVENT);
        }

        private void OnPlayerConnecting([FromSource] Player player, string name, CallbackDelegate kickReason, ExpandoObject deferrals)
        {
            try
            {
                DebugLog($"Connecting: {player.Name}  {player.Identifiers["steam"]}");
                // Check if in queue
                DebugLog($"Currently in queue: {string.Join(", ", _queue.Select(q => q.SteamId))}");
                QueuePlayer queuePlayer = _queue.FirstOrDefault(p => p.SteamId == player.Identifiers["steam"]);

                if (queuePlayer != null)
                {
                    // Player had a slot in the queue, give them it back.
                    DebugLog($"Player found in queue: {queuePlayer.Name}");
                    queuePlayer.Status = QueueStatus.Queued;
                    queuePlayer.ConnectCount++;
                    queuePlayer.Deferrals = deferrals;

                    ((CallbackDelegate)queuePlayer.Deferrals.ToList()[0].Value)();
                    ((CallbackDelegate)queuePlayer.Deferrals.ToList()[2].Value)("Connecting");

                    return;
                }
            
                // Slot available, don't bother with the queue.
                if (this.Players.Count() < _config.MaxClients) return;

                // Check if the player is in the priority list
                PriorityPlayer priorityPlayer = _config.PriorityPlayers.FirstOrDefault(p => p.SteamId == player.Identifiers["steam"]);

                // Add to queue
                queuePlayer = new QueuePlayer()
                {
                    SteamId = player.Identifiers["steam"],
                    Name = player.Name,
                    ConnectCount = 1,
                    ConnectTime = DateTime.UtcNow,
                    Deferrals = deferrals,
                    Priority = priorityPlayer?.Priority ?? 100
                };

                AddToQueue(queuePlayer);
            }
            catch (Exception e)
            {
                Log(e.Message);
                API.CancelEvent();
            }
        }

        private void OnPlayerDropped([FromSource] Player player, string disconnectMessage, CallbackDelegate kickReason)
        {
            try
            {
                DebugLog($"Disconnected: {player.Name}");
                QueuePlayer queuePlayer = _queue.FirstOrDefault(p => p.SteamId == player.Identifiers["steam"]);
                if (queuePlayer == null) return;
                queuePlayer.Status = QueueStatus.Disconnected;
                queuePlayer.DisconnectTime = DateTime.UtcNow;
            }
            catch (Exception e)
            {
                Log(e.Message);
            }
        }

        private void OnPlayerActive([FromSource] Player player)
        {
            try
            {
                QueuePlayer queuePlayer = _queue.FirstOrDefault(p => p.SteamId == player.Identifiers["steam"]);
                if (queuePlayer != null) _queue.Remove(queuePlayer);
            }
            catch (Exception e)
            {
                Log(e.Message);
            }
        }

        private void AddToQueue(QueuePlayer player)
        {
            // Find out where to insert them in the queue.
            int queuePosition = _queue.FindLastIndex(p => p.Priority <= player.Priority) + 1;

            _queue.Insert(queuePosition, player);

            DebugLog($"Added {player.Name} to the queue with priority {player.Priority} [{_queue.IndexOf(player) + 1}/{_queue.Count}]");
            if (player.Deferrals == null) return;
            ((CallbackDelegate)player.Deferrals.ToList()[0].Value)();
            ((CallbackDelegate)player.Deferrals.ToList()[2].Value)("Connecting");
        }


        private async Task ProcessQueue()
        {
            try
            {
                foreach (QueuePlayer queuePlayer in _queue.Where(p => p.Status != QueueStatus.Connecting).ToList())
                {
                    // Let in player if first in queue and server has a slot
                    if (_queue.IndexOf(queuePlayer) == 0 && this.Players.Count() < _config.MaxClients)
                    {
                        ((CallbackDelegate) queuePlayer.Deferrals?.ToList()[1].Value)?.Invoke();
                        queuePlayer.Status = QueueStatus.Connecting;
                        DebugLog($"Letting in player: {queuePlayer.Name}");
                        continue;
                    }
                    // Defer the player until there is a slot available and they're first in queue.
                    if (queuePlayer.Status != QueueStatus.Queued) continue;
                    ((CallbackDelegate) queuePlayer.Deferrals.ToList()[0].Value)();
                    ((CallbackDelegate) queuePlayer.Deferrals.ToList()[2].Value)(
                        $"[{_queue.IndexOf(queuePlayer) + 1}/{_queue.Count}] In queue to connect.{queuePlayer.Dots}");
                    queuePlayer.Dots = new string('.', (queuePlayer.Dots.Length + 1) % 3);
                }
                // Remove players who have been disconnected longer than the grace period
                foreach (QueuePlayer queuePlayer in _queue.Where(p => p.Status == QueueStatus.Disconnected && DateTime.UtcNow.Subtract(p.DisconnectTime).TotalSeconds > _config.DisconnectGrace).ToList())
                {
                    DebugLog($"Disconnect grace expired for player: {queuePlayer.Name}  {queuePlayer.SteamId}");
                    _queue.Remove(queuePlayer);
                }
                // Update the servername
                API.SetConvar("sv_hostname", _queue.Count > 0 ? $"{_config.ServerName} [Queue: {_queue.Count}]" : _config.ServerName);
            }
            catch (Exception e)
            {
                Log(e.Message);
                Log(e.InnerException?.Message);
                DebugLog(e.StackTrace);
            }

            await Delay(500);
        }

        [System.Diagnostics.Conditional("DEBUG")]
        public static void DebugLog(string message)
        {
            Debug.WriteLine($"{DateTime.Now:s} [SERVER:QUEUE]: {message}");
        }

        public static void Log(string message)
        {
            Debug.WriteLine($"{DateTime.Now:s} [SERVER:QUEUE]: {message}");
        }

        public void HandleEvent(string name, Action action) => this.EventHandlers[name] += action;
        public void HandleEvent<T1>(string name, Action<T1> action) => this.EventHandlers[name] += action;
        public void HandleEvent<T1, T2>(string name, Action<T1, T2> action) => this.EventHandlers[name] += action;
        public void HandleEvent<T1, T2, T3>(string name, Action<T1, T2, T3> action) => this.EventHandlers[name] += action;
        public void HandleEvent<T1, T2, T3, T4>(string name, Action<T1, T2, T3, T4> action) => this.EventHandlers[name] += action;
    }
}
