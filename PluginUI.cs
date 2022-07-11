using ImGuiNET;
using System;
using System.Numerics;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using Dalamud.DrunkenToad;
using System.Linq;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using LinkTracker;
using Num = System.Numerics;
using Dalamud.Interface;

namespace LinkTracker
{
    // It is good to have this be disposable in general, in case you ever need it
    // to do any cleanup
    internal class PluginUI : IDisposable
    {
        
        private Configuration Configuration { get; init; }

        public bool isdraw = false;
        public bool namefilter = true;
        public Plugin.MapLink drawlink;
        public Plugin.MapLink lastlink;
        public Plugin.MapLink autolink;
        public double distance;
        public double ldistance=1000;
        public Vector4 linecolor = new Vector4(1f,1f,1f,1f);
        private Plugin Plugin { get; init; }

        private bool trackLinks = true;
        private bool chatLinksVisible = false;
        public bool isautostop;
        public bool isalert;
        public bool showdistance;
        public bool alwaysmap = true;
        public bool arrived;
        public bool showmapdistance;
        public float stopdistance = 40f;
        public float mapdistance = 500f;
        public float linewidth = 10;
        public bool checkall=false;
        public bool uncheckall=false;
        public bool checkallplayer = false;
        public bool uncheckallplayer = false;
        public bool typefilter=false;
        public bool playerfilter=false;


        public Vector4 goldline = new Vector4(229, 204, 128,255);
        public Vector4 pinkline = new Vector4(226, 104, 168, 255);
        public Vector4 orangeline = new Vector4(255, 128, 0, 255);
        public Vector4 purpleline = new Vector4(163, 53, 238, 255);
        public Vector4 blueline = new Vector4(0, 112, 255, 255);
        public Vector4 greenline = new Vector4(30, 255, 0, 255);
        public Vector4 wrongline = new Vector4(102, 102, 102, 255);
        public string typefilterindicater = "";
        public string playerfilterindicater = "";


        public bool ChatLinksVisible
        {
            get => chatLinksVisible;
            set => chatLinksVisible = value;
        }

        public PluginUI(Configuration configuration, Plugin plugin)
        {
            Configuration = configuration;
            Plugin = plugin;
            linewidth = Configuration.linewidth;
            stopdistance = Configuration.stopdistance;
            isautostop = Configuration.isautostop;
            showdistance = Configuration.showdistance;
            mapdistance = Configuration.mapdistance;
            isalert = Configuration.isalert;
            alwaysmap = Configuration.alwaysmap;
            Plugin.mutesonar = Configuration.mutesonar;
            showmapdistance = Configuration.showmapdistance;
            Plugin.isautotrack = Configuration.isautotrack;

        }

        public void Dispose()
        {
        }

        public void Draw()
        {
            // This is our only draw handler attached to UIBuilder, so it needs to be
            // able to draw any windows we might have open.
            // Each method checks its own visibility/state to ensure it only draws when
            // it actually makes sense.
            // There are other ways to do this, but it is generally best to keep the number of
            // draw delegates as low as possible.

            DrawChatLinksWindow();
            if (drawlink!=null && filterer(drawlink))
                isdraw = false;

            
            if (Plugin.isautotrack && lastlink!=null)
            {
                autolink = null;
                for (int i = Plugin.Link.Count - 1; i >= 0; i--)
                {
                    if (!filterer(Plugin.Link[i]))
                    {
                        autolink = Plugin.Link[i];
                        break;
                    }
                    
                }
                if (autolink != null)
                {
                    autotrack();
                    drawline();
                }
                
            }
        }

        public void DrawChatLinksWindow()
        {
            if (!chatLinksVisible)
            {
                return;
            }
            try
            {
                
                ImGui.SetNextWindowSize(new Vector2(375, 330), ImGuiCond.FirstUseEver);
                ImGui.SetNextWindowSizeConstraints(new Vector2(375, 330), new Vector2(float.MaxValue, float.MaxValue));
                
                if (ImGui.Begin("Link Tracker", ref chatLinksVisible))
                {
                    
                    ImGui.BeginTabBar("LineSetting");
                    this.drawlinks();
                    this.drawconfigs();
                    this.drawfilter();
                    this.drawnamefilter();
                    ImGui.EndTabBar();
                    if(!Plugin.isautotrack)
                        this.drawline();

                    
                    

                }

            }

            catch (KeyNotFoundException)
            {
                // this happens when we remove the last item in a list for a given key in the dictionary
                // we don't want to crash, just keep rendering
            }

        }

        private void drawlinks()
        {
            if (!ImGui.BeginTabItem("Links"))
                return;
            
            if(showdistance && distance!=0)
                ImGui.Text(distance.ToString());
            if(ImGui.Checkbox("AutoTrack", ref Plugin.isautotrack))
            {
                Plugin.Chat.Print("click");
                if (!Plugin.isautotrack)
                {            
                    isdraw = false;
                }

                    
            }
            ImGui.SameLine();
            if (ImGui.Button($"Clear Links"))
            {
                isdraw = false;
                Plugin.Link.Clear();
            }
                
            

            if (ImGui.BeginTable("Links", 4, ImGuiTableFlags.Resizable))
            {
                if (typefilter)
                    typefilterindicater = "(filtered)";
                else
                    typefilterindicater = "";

                if (playerfilter)
                    playerfilterindicater = "(filtered)";
                else
                    playerfilterindicater = "";
                
                ImGui.TableSetupColumn(typefilterindicater+"Type");
                ImGui.TableSetupColumn(playerfilterindicater+"Name");
                ImGui.TableSetupColumn("Message");
                ImGui.TableSetupColumn("Link");
                ImGui.TableHeadersRow();
                try
                {
                    int templink = 0;
                    for (int i = Plugin.Link.Count - 1; i >= 0; i--)
                    {
                        string thetype = "";
                        string thename = "";

                         Plugin.MapLink link = Plugin.Link[i];

                        bool filtered = true;
                        foreach (var types in link.ctype)
                        {
                            if (Plugin.xivchattype[types])
                            {
                                thetype = types.ToString();
                                filtered = false;
                                break;
                            }

                        }
                        if (filtered)
                            continue;
                        filtered = true;
                        foreach (var player in link.name)
                        {
                            if (Plugin.playerlist.ContainsKey(player) && Plugin.playerlist[player])
                            {
                                thename = player.ToString();
                                filtered = false;
                                break;
                            }
                        }

                        if (filtered)
                            continue;
                        templink++;
                        if (templink == 1)
                            lastlink = link;
                        string press;
                        if (link.pressed)
                            press = "√";
                        else
                            press = "";

                        ImGui.TableNextRow();
                        _ = ImGui.TableNextColumn();
                        ImGui.Text($"({link.ctype.Count}){thetype}");
                        _ = ImGui.TableNextColumn();
                        //ImGui.Text(link.name);

                        if (ImGui.Button($"({link.name.Count}){thename}"))
                        {
                            namefilter = !namefilter;
                            
                            foreach (var player in Plugin.playerlist.Keys)
                            {
                                Plugin.playerlist[player] = namefilter;
                            }

                            foreach (var player in link.name)
                            {
                                Plugin.playerlist[player] = true;
                            }

                        }
                        _ = ImGui.TableNextColumn();
                        if (ImGui.Button($"{press}{link.message}"))
                        {
                            Plugin.isautotrack = false;
                            link.pressed = true;
                            //Plugin.Chat.Print($"{link.location},{Plugin.currentterritory}");


                            if (isdraw && (drawlink.link.PlaceName + drawlink.link.CoordinateString).Equals(link.link.PlaceName + link.link.CoordinateString))
                                isdraw = !isdraw;
                            else
                                isdraw = true;
                            arrived = false;
                            drawlink = link;


                            //Plugin.Chat.Print($"X:{link.link.XCoord},Y:{link.link.YCoord},mapdistance:{link.link.CoordinateString}");
                            if (Plugin.ClientState.LocalPlayer != null)
                                distance = Math.Sqrt(Math.Abs(Math.Pow(Plugin.ClientState.LocalPlayer.Position.X - (link.link.RawX / 1000), 2) + Math.Pow(Plugin.ClientState.LocalPlayer.Position.Z - (link.link.RawY / 1000), 2)));
                            if (alwaysmap || link.location != Plugin.currentterritory || (showmapdistance && distance >= mapdistance))
                                _ = Plugin.GameGui.OpenMapWithMapLink(link.link);
                            //Plugin.Chat.Print(spo.X + "," + spo.Y);
                            //Plugin.Chat.Print(spo2.X + "," + spo2.Y);
                            //Plugin.Chat.Print(drawlink.link.RawX.ToString() + "," + drawlink.link.RawY.ToString());

                        }
                        if (ImGui.IsItemHovered())
                            ImGui.SetTooltip($"{link.message}");
                        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                        {
                            Plugin.isautotrack = false;
                            _ = Plugin.GameGui.OpenMapWithMapLink(link.link);
                            drawlink = link;
                            isdraw = true;
                        }
                        _ = ImGui.TableNextColumn();
                        ImGui.Text($"{link.link.PlaceName} {link.link.CoordinateString}");
                    }
                }
                catch (Exception)
                {
                    // this happens when we remove the last item in a list for a given key in the dictionary
                    // we don't want to crash, just keep rendering
                }
                ImGui.EndTable();
            }
            ImGui.EndTabItem();
        }
        private void drawconfigs()
        {
            if (!ImGui.BeginTabItem("Configs"))
                return;
            ImGui.InputFloat("Width", ref linewidth);
            ImGui.Checkbox("Autostop", ref isautostop);
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Automatically stop tracking when you reach the target distance");
            ImGui.SameLine();
            ImGui.Checkbox("Arrival Reminder", ref isalert);
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Show a message when you reach the target distance");
            if(isautostop || isalert)
                ImGui.InputFloat("target distance", ref stopdistance);
            ImGui.Checkbox("Always show map", ref alwaysmap);
            if (!alwaysmap)
            {
                ImGui.Checkbox("Show map over distance", ref showmapdistance);
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Automatically open the map when the link is too far away");
                if (showmapdistance)
                    ImGui.InputFloat("Show map distance", ref mapdistance);
            }
            ImGui.Checkbox("Show distance", ref showdistance);
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Show the distance on top");
            ImGui.Checkbox("Mute Sonar", ref Plugin.mutesonar);
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Sonar shutup!");
            if (ImGui.Button("Save"))
            {
                saveconfig();
            }

            ImGui.EndTabItem();
        }

        public void drawfilter()
        {
            if (!ImGui.BeginTabItem("TypeFilter"))
                return;
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Toggle to show the link from that source, some will not be used");
            int i = 0;
            if (ImGui.Button("All"))
            {
                saveconfig();
                checkall = true;
            }
            ImGui.SameLine();
            if (ImGui.Button("None"))
            {
                saveconfig();
                uncheckall = true;
            }
            bool tempfilter = false;
            
            foreach (var types in Plugin.xivchattype)
            {
                bool a = types.Value;
                var b = types.Key;
                if ((i + 1) % 4 != 0 && i!=0)
                    ImGui.SameLine();



                if (ImGui.Checkbox($"{b.ToString()}###{b.ToString()}", ref a))
                {
                    //Plugin.Chat.Print("1");
                    saveconfig();

                }
                


                Plugin.xivchattype[b] = a;
                if (checkall)
                {
                    Plugin.xivchattype[b] = true;
                    
                }
                if (uncheckall)
                {
                    Plugin.xivchattype[b] = false;
                    
                }
                if(Plugin.xivchattype[b] == false)
                    tempfilter = true;
                i++;
            }

            typefilter = tempfilter;
            if (checkall)
            {
                checkall = false;

            }
            if (uncheckall)
            {
                uncheckall = false;

            }

            ImGui.EndTabItem();
        }

        public void drawnamefilter()
        {
            if (!ImGui.BeginTabItem("NameFilter"))
                return;
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Toggle to show the link from that player");
            if (ImGui.Button("All players"))
            {
                checkallplayer = true;
            }
            ImGui.SameLine();
            if (ImGui.Button("None player"))
            {
                uncheckallplayer = true;
            }
            ImGui.SameLine();
            if (ImGui.Button("Clear All"))
            {
                Plugin.playerlist.Clear();
            }
            if (ImGui.BeginTable("Players", 2, ImGuiTableFlags.Resizable))
            {
                ImGui.TableSetupColumn("Name");
                ImGui.TableSetupColumn("");
                ImGui.TableHeadersRow();



                try
                {
                    bool tempfilter = false;
                    foreach (var player in Plugin.playerlist.Reverse())
                    {
                        bool a = player.Value;
                        var b = player.Key;

                        ImGui.TableNextRow();
                        _ = ImGui.TableNextColumn();


                        ImGui.Checkbox(player.Key.ToString(), ref a);


                        
                        Plugin.playerlist[b] = a;
                        if (checkallplayer)
                        {
                            Plugin.playerlist[b] = true;

                        }
                        if (uncheckallplayer)
                        {
                            Plugin.playerlist[b] = false;

                        }
                        if(Plugin.playerlist[b] == false)
                            tempfilter = true;
                        _ = ImGui.TableNextColumn();
                        if (ImGui.Button($"Remove###{b}"))
                        {
                            Plugin.playerlist.Remove(b);
                        }


                    }
                    playerfilter = tempfilter;
                }
                catch (KeyNotFoundException)
                {
                    // this happens when we remove the last item in a list for a given key in the dictionary
                    // we don't want to crash, just keep rendering
                }
                
                if (checkallplayer)
                {
                    checkallplayer = false;

                }
                if (uncheckallplayer)
                {
                    uncheckallplayer = false;

                }

                
                ImGui.EndTable();
            }
            ImGui.EndTabItem();

        }


        public Vector4 transformcolor(Vector4 color)
        {
            Vector4 colors = new Vector4(color.X / 255, color.Y / 255, color.Z / 255, color.W / 255);
            return colors;
        }

        public Vector4 switchcolor(double distance)
        {
            Vector4 color = new Vector4();
            if (distance <= stopdistance + 100)
                color = goldline;
            else if (distance <= stopdistance + 200)
                color = pinkline;
            else if (distance <= stopdistance + 300)
                color = orangeline;
            else if (distance <= stopdistance + 400)
                color = purpleline;
            else if (distance <= stopdistance + 500)
                color = blueline;
            else if (distance > stopdistance + 500)
                color = greenline;
            return transformcolor(color);
        }

        public void saveconfig()
        {
            Configuration.linewidth = linewidth;
            Configuration.stopdistance = stopdistance;
            Configuration.isautostop = isautostop;
            Configuration.showdistance = showdistance;
            Configuration.mapdistance = mapdistance;
            Configuration.isalert = isalert;
            Configuration.alwaysmap=alwaysmap;
            Configuration.mutesonar = Plugin.mutesonar;
            Configuration.isautotrack = Plugin.isautotrack;
            Configuration.showmapdistance=showmapdistance;
            foreach(var type in Plugin.xivchattype.Keys)
            {
                if (Configuration.typedic.ContainsKey(type))
                {
                    
                    Configuration.typedic[type] = Plugin.xivchattype[type];
                    //Plugin.Chat.Print($"1,{type},{Configuration.typedic[type]},{Plugin.xivchattype[type]}");
                }

                else {
                    
                    Configuration.typedic.Add(type, Plugin.xivchattype[type]);
                    //Plugin.Chat.Print($"2,{type},{Configuration.typedic[type]},{Plugin.xivchattype[type]}");
                }
                    
            }

            Plugin.DalamudPluginInterface.SavePluginConfig(Configuration);

        }

        public void autotrack()
        {
            autolink.pressed = true;
            if (drawlink != null && !filterer(drawlink))
            {
                
                //Plugin.Chat.Print($"{link.location},{Plugin.currentterritory}");
                isdraw = true;

                if (isdraw && (autolink.link.PlaceName + autolink.link.CoordinateString).Equals(drawlink.link.PlaceName + drawlink.link.CoordinateString))
                    return;
                
            }
            drawlink = autolink;
            arrived = false;

            //Plugin.Chat.Print($"X:{link.link.XCoord},Y:{link.link.YCoord},mapdistance:{link.link.CoordinateString}");
            if (Plugin.ClientState.LocalPlayer != null)
                distance = Math.Sqrt(Math.Abs(Math.Pow(Plugin.ClientState.LocalPlayer.Position.X - (drawlink.link.RawX / 1000), 2) + Math.Pow(Plugin.ClientState.LocalPlayer.Position.Z - (drawlink.link.RawY / 1000), 2)));
            if (alwaysmap || drawlink.location != Plugin.currentterritory || (showmapdistance && distance >= mapdistance))
                _ = Plugin.GameGui.OpenMapWithMapLink(drawlink.link);
        }
        public void drawline()
        {
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Num.Vector2(0, 0));
            ImGuiHelpers.ForceNextWindowMainViewport();
            ImGuiHelpers.SetNextWindowPosRelativeMainViewport(new Num.Vector2(0, 0));
            ImGui.Begin("Rings",
ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoNav | ImGuiWindowFlags.NoTitleBar |
ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoBackground);


            ImGui.SetWindowSize(ImGui.GetIO().DisplaySize);
            if (isdraw && Plugin.ClientState.LocalPlayer != null && drawlink.location == Plugin.currentterritory)
            {
                distance = Math.Sqrt(Math.Abs(Math.Pow(Plugin.ClientState.LocalPlayer.Position.X - (drawlink.link.RawX / 1000), 2) + Math.Pow(Plugin.ClientState.LocalPlayer.Position.Z - (drawlink.link.RawY / 1000), 2)));


                if (distance > ldistance)
                    linecolor = transformcolor(wrongline);
                else
                    linecolor = switchcolor(distance);
                Plugin.GameGui.WorldToScreen(Plugin.ClientState.LocalPlayer.Position, out Vector2 spo);
                Plugin.GameGui.WorldToScreen(new Vector3(drawlink.link.RawX / 1000, Plugin.ClientState.LocalPlayer.Position.Y, drawlink.link.RawY / 1000), out Vector2 spo2);

                ImGui.GetWindowDrawList().AddLine(new Vector2(spo.X, spo.Y), new Vector2(spo2.X, spo2.Y), ImGui.GetColorU32(linecolor), linewidth);

                ldistance = distance;
                if (distance <= stopdistance)
                {
                    if (isalert && !arrived)
                    {
                        Plugin.Chat.Print("Arrived!");
                        arrived = true;
                    }

                    if (isautostop)
                        isdraw = false;
                }



            }

            ImGui.PopStyleVar();
        }
        public bool filterer(Plugin.MapLink link)
        {
            if(!typefilter && !playerfilter)
                return false;
            bool filtered = true;
            foreach (var types in link.ctype)
            {
                if (Plugin.xivchattype[types])
                {
                    
                    filtered = false;
                    break;
                }

            }
            if (filtered)
                return filtered;
            filtered = true;
            foreach (var player in link.name)
            {
                if (Plugin.playerlist.ContainsKey(player) && Plugin.playerlist[player])
                {
                    
                    filtered = false;
                    break;
                }
            }
            return filtered;
        }
    }
}
