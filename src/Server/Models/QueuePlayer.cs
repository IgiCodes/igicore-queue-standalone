using System;
using System.Dynamic;
using CitizenFX.Core;

namespace IgiCore_Queue.Server.Models
{
    public class QueuePlayer
    {
        public string SteamId;
        public string Name;
        public ExpandoObject Deferrals;
        public string Dots = "";
        public QueueStatus Status = QueueStatus.Queued;
        public DateTime ConnectTime;
        public int ConnectCount;
        public DateTime DisconnectTime;
    }
}