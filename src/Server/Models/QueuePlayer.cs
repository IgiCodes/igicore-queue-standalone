using System;
using System.Dynamic;
using CitizenFX.Core;

namespace IgiCore_Queue.Server.Models
{
    public class QueuePlayer : IPlayer
    {
        public ExpandoObject Deferrals { get; set; }
        public string Dots { get; set; } = "";
        public QueueStatus Status { get; set; } = QueueStatus.Queued;
        public DateTime ConnectTime { get; set; }
        public int ConnectCount { get; set; }
        public DateTime DisconnectTime { get; set; }
        public int Priority { get; set; }
        public string SteamId { get; set; }
        public string Name { get; set; }
    }
}