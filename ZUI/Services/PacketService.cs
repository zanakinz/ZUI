using System;
using System.Collections.Generic;
using System.Text.Json;
using ZUI.API;
using ZUI.Utils;
using ZUI.UI.ModContent;

namespace ZUI.Services
{
    public static class PacketService
    {
        public const string PACKET_PREFIX = "[[ZUI]]";

        public class ZuiPacket
        {
            public string Type { get; set; }
            public string Plugin { get; set; }
            public string Window { get; set; }
            public Dictionary<string, string> Data { get; set; }
        }

        public static bool TryProcessPacket(string message)
        {
            // 1. Check Prefix
            if (string.IsNullOrEmpty(message) || !message.StartsWith(PACKET_PREFIX))
                return false;

            try
            {
                // 2. Extract JSON payload
                string json = message.Substring(PACKET_PREFIX.Length);

                // 3. Deserialize
                var packet = JsonSerializer.Deserialize<ZuiPacket>(json);
                if (packet == null) return false;

                // 4. Set Context
                if (!string.IsNullOrEmpty(packet.Plugin))
                {
                    ModRegistry.SetPlugin(packet.Plugin);
                }

                if (!string.IsNullOrEmpty(packet.Window))
                {
                    ModRegistry.SetTargetWindow(packet.Window);
                }

                // 5. Execute Command
                ExecuteCommand(packet);

                return true; // Packet handled, consume message
            }
            catch (Exception ex)
            {
                LogUtils.LogError($"[PacketService] Failed to parse server packet: {ex.Message}");
                // Return true to consume the message anyway, so the user doesn't see broken JSON in chat
                return true;
            }
        }

        private static void ExecuteCommand(ZuiPacket p)
        {
            var d = p.Data; // Short alias for readability

            switch (p.Type)
            {
                case "RegisterImage":
                    if (d.ContainsKey("Name") && d.ContainsKey("Url"))
                    {
                        // Trigger background download
                        ImageDownloader.Download(d["Name"], d["Url"]);
                    }
                    break;

                case "CreateTab":
                    if (d.ContainsKey("Name"))
                    {
                        string tip = d.ContainsKey("Tip") ? d["Tip"] : "";
                        ModRegistry.CreateTab(d["Name"], tip);
                    }
                    break;

                case "SetUITemplate":
                    if (d.ContainsKey("Template"))
                        ModRegistry.SetUITemplate(d["Template"]);
                    break;

                case "SetUICustom":
                    if (d.ContainsKey("W") && d.ContainsKey("H"))
                        ModRegistry.SetUICustom(ParseInt(d["W"]), ParseInt(d["H"]));
                    break;

                case "SetTitle":
                    if (d.ContainsKey("Text"))
                        ModRegistry.SetWindowTitle(d["Text"]);
                    break;

                case "HideTitleBar":
                    ModRegistry.HideTitleBar();
                    break;

                case "AddCategory":
                    if (d.ContainsKey("Name"))
                        ModRegistry.AddCategory(d["Name"], ParseFloat(d, "X", -1), ParseFloat(d, "Y", -1));
                    break;

                case "AddText":
                    if (d.ContainsKey("Text"))
                        ModRegistry.AddText(d["Text"], ParseFloat(d, "X", -1), ParseFloat(d, "Y", -1));
                    break;

                case "AddButton":
                    if (d.ContainsKey("Text") && d.ContainsKey("Cmd"))
                    {
                        string tooltip = d.ContainsKey("Tip") ? d["Tip"] : "";
                        float x = ParseFloat(d, "X", -1);
                        float y = ParseFloat(d, "Y", -1);

                        // Check for Custom Image/Size Overload
                        if (d.ContainsKey("Img") && d.ContainsKey("W") && d.ContainsKey("H"))
                        {
                            // NOTE: Server cannot send an Assembly. 
                            // Passing 'null' as assembly forces SpriteLoader to look in global/shared paths or Manual Registry.
                            ModRegistry.AddButton(null, d["Text"], d["Cmd"], d["Img"], x, y, ParseFloat(d, "W"), ParseFloat(d, "H"));
                        }
                        else if (d.ContainsKey("W") && d.ContainsKey("H"))
                        {
                            // Custom Size, Default Image (pass null for imageName)
                            ModRegistry.AddButton(null, d["Text"], d["Cmd"], null, x, y, ParseFloat(d, "W"), ParseFloat(d, "H"));
                        }
                        else
                        {
                            // Standard Button
                            ModRegistry.AddButton(d["Text"], d["Cmd"], tooltip, x, y);
                        }
                    }
                    break;

                case "AddImage":
                    if (d.ContainsKey("Img") && d.ContainsKey("W") && d.ContainsKey("H"))
                    {
                        // Pass null assembly for server requests (Global/Manual lookup)
                        ModRegistry.AddImage(null, d["Img"], ParseFloat(d, "X"), ParseFloat(d, "Y"), ParseFloat(d, "W"), ParseFloat(d, "H"));
                    }
                    break;

                case "AddCloseButton":
                    string txt = d.ContainsKey("Text") ? d["Text"] : "Close";
                    ModRegistry.AddCloseButton(txt, ParseFloat(d, "X", -1), ParseFloat(d, "Y", -1));
                    break;

                case "Open":
                    // Force open the window we just configured
                    ModRegistry.OpenWindow(p.Plugin, p.Window);
                    break;
            }
        }

        // Helpers to parse dictionary strings safely
        private static int ParseInt(string val) => int.TryParse(val, out int result) ? result : 0;

        private static float ParseFloat(Dictionary<string, string> d, string key, float defaultVal = 0f)
        {
            float result;
            if (d.TryGetValue(key, out string val) && float.TryParse(val, out result))
                return result;

            return defaultVal;
        }
    }
}