namespace IgiCore_Queue.Server.Models
{
    public class PriorityPlayer : IPlayer
    {
        public int Priority { get; set; }
        public string SteamId { get; set; }
        public string Name { get; set; }
    }
}