using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;

namespace IgiCore_Queue.Client
{
    public class Client : BaseScript
    {
        private static bool IsConnected { get; set; } = false;

        public Client()
        {
            Tick += OnTick;
        }

        private static async Task OnTick()
        {
            if (NetworkIsSessionStarted())
            {
                if (!Client.IsConnected)
                {
                    Client.IsConnected = true;
                    TriggerServerEvent("igicore:queue:playerActive");
                }
            }
            await Delay(1);
        }
    }

    
}
