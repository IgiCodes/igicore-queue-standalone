using System;
using System.Collections.Generic;
using System.IO;
using CitizenFX.Core.Native;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace IgiCore_Queue.Server.Models
{
    public class Config
    {
        public List<PriorityPlayer> PriorityPlayers { get; set; } = new List<PriorityPlayer>();
        public int DisconnectGrace { get; set; } = 60;
        public int ConnectionTimeout { get; set; } = 120;
        public int MaxClients { get; set; }
        public bool QueueWhenNotFull { get; set; } = false;
        public string ServerName { get; set; }

        public Config()
        {
            MaxClients = API.GetConvarInt("sv_maxclients", 32);
            ServerName = API.GetConvar("sv_hostname", "");
        }

        public static Config Load(string configPath)
        {
            Config config = new Config();
            try
            {
                Deserializer deserializer = new DeserializerBuilder()
                    .WithNamingConvention(new PascalCaseNamingConvention()).Build();

                config = deserializer.Deserialize<Config>(File.ReadAllText(configPath));
                
            }
            catch (Exception e)
            {
                Server.Log($"Failed to load config file: {configPath}");
                Server.Log(e.Message);
                Server.Log(e.InnerException?.Message);
                Server.DebugLog(e.StackTrace);
            }

            Server.Log("Config Loaded");

            return config;
        }
    }
}