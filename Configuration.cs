using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using Dalamud.Game.Text;
using System.Collections.Generic;
namespace LinkTracker
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;
        public bool mutesonar { get; set; } = true;

        public bool isautostop { get; set; } = false;
        public bool isalert { get; set; } = false;
        public bool showdistance { get; set; } = false;
        public bool alwaysmap { get; set; } = true;
  
        public bool showmapdistance { get; set; }=false;
        public float stopdistance { get; set; } = 40f;
        public float mapdistance { get; set; } = 500f;
        public float linewidth { get; set; } = 10;
        public bool isautotrack { get; set; } = false;

        public Dictionary<XivChatType, bool> typedic { get; set; }

        // the below exist just to make saving less cumbersome

        [NonSerialized]
        private DalamudPluginInterface? pluginInterface;

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
        }

        

        public void Save()
        {
            pluginInterface!.SavePluginConfig(this);

        }
    }
}
