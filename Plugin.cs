using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.Reflection;
using Dalamud.Game.Gui;
using System.Collections.Generic;
using Dalamud.Data;
using System;
using System.Linq;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Fates;
using Dalamud.Game.ClientState;
using Dalamud.Game;
using System.Threading;
using ImGuiNET;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.DrunkenToad;
using System.Collections.Specialized;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.GeneratedSheets;

namespace LinkTracker
{
    public class Plugin : IDalamudPlugin
    {
        public string Name => "Link Tracker";
        
        private Configuration Configuration { get; init; }
        private PluginUI PluginUi { get; init; }

        [PluginService] public static ChatGui Chat { get; private set; } = null!;
        [PluginService] public static GameGui GameGui { get; private set; } = null!;
        [PluginService] public static DataManager DataManager { get; private set; } = null!;
        [PluginService] public static FateTable FateTable { get; private set; } = null!;
        [PluginService] public static Framework Framework { get; private set; } = null!;
        [PluginService] public static ClientState ClientState { get; private set; } = null!;
        [PluginService] public static DalamudPluginInterface DalamudPluginInterface { get; private set; } = null!;
        [PluginService] public static CommandManager CommandManager { get; private set; } = null!;
        public string currentterritory;
        public Dictionary<XivChatType, bool> xivchattype = new();
        public Dictionary<string, bool> playerlist = new();
        public bool mutesonar = true;
        public bool isautotrack = false;

        public class MapLink
        {
            public MapLink(XivChatType ctype,string name, SeString message,string location, MapLinkPayload link)
            {
                this.ctype = new List<XivChatType>();
                this.ctype.Add(ctype);
                this.name = new List<string>();
                this.name.Add(name);
                this.message = message;
                this.location = location;
                this.link = link;
                this.pressed = false;
            }
            public List<XivChatType> ctype;
            public List<string> name;
            public SeString message;
            public string location;
            public MapLinkPayload link;
            public bool pressed = false;
            
            


        }

        public Dictionary<string, List<MapLink>> Links = new();
        public List<MapLink> Link = new();

        //private List<MapLink>? links = null;
        //public List<MapLink> Links => links ??= new List<MapLink>();


        public Plugin()
        {
            Configuration = DalamudPluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Configuration.Initialize(DalamudPluginInterface);

            // you might normally want to embed resources and load them from the manifest stream
            string? assemblyLocation = Assembly.GetExecutingAssembly().Location;
            PluginUi = new PluginUI(Configuration, this);






            _ = CommandManager.AddHandler("/xllt", new CommandInfo(OnMapNightCommand)
            {
                HelpMessage = "Opens the map night links window",
                ShowInHelp = true
            });

            Chat.ChatMessage += PollChatMessage;

            DalamudPluginInterface.UiBuilder.Draw += DrawUI;
            ClientState.TerritoryChanged += TerritoryChanged;
            TerritoryChanged(this, ClientState.TerritoryType);


            foreach (XivChatType types in Enum.GetValues(typeof(XivChatType)))
            {
                xivchattype.Add(types, true);
                //Chat.Print($"{types}");
            }
            foreach (var type in xivchattype.Keys)
            {
                //Plugin.Chat.Print($"1,{type},{Configuration.typedic[type]},{xivchattype[type]}");
                if(Configuration.typedic.ContainsKey(type))
                    xivchattype[type] = Configuration.typedic[type];
            }

        }

        private void TerritoryChanged(object sender, ushort e)
        {
            try
            {

                var territory = DataManager.GetExcelSheet<TerritoryType>()?.GetRow(e);
                currentterritory = territory.PlaceName.Value?.Name?.ToString();
                //Chat.Print($"change to {currentterritory}");
            }
            catch (KeyNotFoundException)
            {
                Chat.Print("Could not get territory for current zone");
            }
        }

        public void PollChatMessage(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
        {
            if (mutesonar && sender.TextValue.ToLower() == "sonar")
            {
                return;
            }
            Payload? link = message.Payloads.Find(i => i.Type == PayloadType.MapLink);
            if (link != null)
            {


                //if (!Links.ContainsKey(((MapLinkPayload)link).PlaceName))
                //{
                //    Links.Add(((MapLinkPayload)link).PlaceName, new());
                //}
                if (!playerlist.ContainsKey(sender.TextValue))
                    playerlist.Add(sender.TextValue, true);
                string cor = ((MapLinkPayload)link).PlaceName + ((MapLinkPayload)link).CoordinateString;
                //Links[((MapLinkPayload)link).PlaceName].Add(new MapLink(type,sender.TextValue[0..], message.TextValue,((MapLinkPayload)link).PlaceName, (MapLinkPayload)link));
                for (int i = Link.Count-1;i>=0;i--)
                {
                    if (i < Link.Count - 50)
                        break;
                    if ((Link[i].link.PlaceName + Link[i].link.CoordinateString).Equals(cor))
                    {
                        if (Link[i].ctype.IndexOf(type) == -1)
                            Link[i].ctype.Add(type);
                        if (Link[i].name.IndexOf(sender.TextValue) == -1)
                            Link[i].name.Add(sender.TextValue);
                        return;
                    }
                        

                }
                MapLink newlink = new MapLink(type, sender.TextValue, message, ((MapLinkPayload)link).PlaceName, (MapLinkPayload)link);
                Link.Add(newlink);
                if (!PluginUi.filterer(newlink))
                {
                    PluginUi.autolink = newlink;
                    
                }
                    
                if (Link.Count >= 100)
                {
                    for(int i =0;i<30;i++)
                    {
                        Link.Remove(Link[i]);
                    }
                }
            }
        }

        private void OnMapNightCommand(string command, string arguments)
        {
            DrawMapNightUI();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA1816:Dispose methods should call SuppressFinalize", Justification = "Won't change")]
        public void Dispose()
        {
            PluginUi.Dispose();
            Chat.ChatMessage -= PollChatMessage;
            _ = CommandManager.RemoveHandler("/xllt");
        }

        private void DrawUI()
        {
            PluginUi.Draw();
        }

        private void DrawMapNightUI()
        {
            PluginUi.ChatLinksVisible = true;
        }
    }
}
