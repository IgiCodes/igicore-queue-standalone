using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using IgiCore_Queue.Server.Extensions;
using IgiCore_Queue.Server.Models;

namespace IgiCore_Queue.Server
{
    public class Server : BaseScript
    {
        private readonly List<QueuePlayer> _queue = new List<QueuePlayer>();
        private static int _maxClients;
        private static int _disconnectGrace;
        private static string _serverName;

        public Server()
        {
            try
            {
                _maxClients = API.GetConvarInt("sv_maxclients", 32);
                _disconnectGrace = API.GetConvarInt("igi_queue_disconnectGrace", 60);
                _serverName = API.GetConvar("sv_hostname", "");

                DebugLog($"Max Clients: {_maxClients}");
                DebugLog($"Disconnect Grace: {_disconnectGrace}");
                HandleEvent<Player, string, CallbackDelegate, ExpandoObject>("playerConnecting", OnPlayerConnecting);
                HandleEvent<Player, string, CallbackDelegate>("playerDropped", OnPlayerDropped);
                HandleEvent<Player>("igicore:queue:playerActive", OnPlayerActive);
                Tick += ProcessQueue;
            }
            catch (Exception e)
            {
                Log(e.Message);
                throw;
            }
            
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
                if (this.Players.Count() < _maxClients) return;

                // Add to queue
                queuePlayer = new QueuePlayer()
                {
                    SteamId = player.Identifiers["steam"],
                    Name = player.Name,
                    ConnectCount = 1,
                    ConnectTime = DateTime.UtcNow,
                    Deferrals = deferrals
                };    
                _queue.Add(queuePlayer);

                DebugLog($"Added {name} to the queue [{_queue.IndexOf(queuePlayer) + 1}/{_queue.Count}]");
                ((CallbackDelegate)queuePlayer.Deferrals.ToList()[0].Value)();
                ((CallbackDelegate)queuePlayer.Deferrals.ToList()[2].Value)("Connecting");

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


        private async Task ProcessQueue()
        {
            try
            {
                foreach (QueuePlayer queuePlayer in _queue.Where(p => p.Status != QueueStatus.Connecting).ToList())
                {
                    // Let in player if first in queue and server has a slot
                    if (_queue.IndexOf(queuePlayer) == 0 && this.Players.Count() < _maxClients)
                    {
                        ((CallbackDelegate)queuePlayer.Deferrals.ToList()[1].Value)();
                        queuePlayer.Status = QueueStatus.Connecting;
                        DebugLog($"Letting in player: {queuePlayer.Name}");
                        continue;
                    }
                    // Defer the player until there is a slot available and they're first in queue.
                    ((CallbackDelegate)queuePlayer.Deferrals.ToList()[0].Value)();
                    ((CallbackDelegate)queuePlayer.Deferrals.ToList()[2].Value)($"[{_queue.IndexOf(queuePlayer) + 1}/{_queue.Count}] In queue to connect.{queuePlayer.Dots}");
                    queuePlayer.Dots = new string('.', (queuePlayer.Dots.Length + 1) % 3);
                }

                // Remove players who have been disconnected longer than the grace period
                foreach (QueuePlayer queuePlayer in _queue.Where(p => p.Status == QueueStatus.Disconnected && DateTime.UtcNow.Subtract(p.DisconnectTime).TotalSeconds > _disconnectGrace).ToList())
                {
                    _queue.Remove(queuePlayer);
                }

                // Update the servername
                API.SetConvar("sv_hostname", _queue.Count > 0 ? $"{_serverName} [Queue: {_queue.Count}]" : _serverName);
            }
            catch (Exception e)
            {
                Log(e.Message);
            }

            await Delay(500);
        }

        [System.Diagnostics.Conditional("DEBUG")]
        private static void DebugLog(string message)
        {
            Debug.WriteLine($"{DateTime.Now:s} [SERVER:QUEUE]: {message}");
        }

        private static void Log(string message)
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
