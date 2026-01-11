using ProjectM.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ZUI.Config;
using ZUI.UI.ModContent;
using Unity.Entities;
using ZUI.Utils;
using ProjectM;

namespace ZUI.Services
{
    internal static partial class MessageService
    {
        private static AbilitySchoolType? _oldVersionColor;
        private static string _oldVersionName;
        public const string BCCOM_LISTBOXES1 = ".fam boxes";
        public const string BCCOM_LISTBOXES2 = ".familiar listboxes";
        public const string BCCOM_SWITCHBOX = ".fam cb {0}";
        public const string BCCOM_BOXCONTENT = ".fam l";
        public const string BCCOM_BINDFAM = ".fam b {0}";
        public const string BCCOM_UNBINDFAM = ".fam ub";
        public const string BCCOM_FAMSTATS = ".fam gl";
        public const string BCCOM_COMBAT = ".fam c";
        public const string BCCOM_ENABLEEQUIP = ".fam e";
        public const string BCCOM_TOGGLEFAM = ".fam toggle";
        public const string BCCOM_DELETEFAM = ".fam r {0}";
        public const string BCCOM_PRESTIGEFAM = ".fam pr";
        
        // Quest Commands
        public const string BCCOM_QUEST_TRACK_DAILY = ".quest track daily";
        public const string BCCOM_QUEST_TRACK_WEEKLY = ".quest track weekly";
        public const string BCCOM_QUEST_REROLL_DAILY = ".quest reroll daily";
        public const string BCCOM_QUEST_REROLL_WEEKLY = ".quest reroll weekly";
        public const string BCCOM_QUEST_PROGRESS_DAILY = ".quest p daily";
        public const string BCCOM_QUEST_PROGRESS_WEEKLY = ".quest p weekly";
        
        // Class Commands
        public const string BCCOM_CLASS_LIST = ".class list";
        public const string BCCOM_CLASS_SELECT = ".class select {0}";
        public const string BCCOM_CLASS_CHANGE = ".class change {0}";
        public const string BCCOM_CLASS_LISTSTATS = ".class liststats {0}";
        
        // Weapon Commands
        public const string BCCOM_WEAPON_GET = ".weapon get";
        public const string BCCOM_WEAPON_LISTSTATS = ".weapon liststats";
        public const string BCCOM_WEAPON_CHOOSESTAT = ".weapon choosestat {0} {1}";
        
        // Blood Commands
        public const string BCCOM_BLOOD_GET = ".blood get {0}";
        public const string BCCOM_BLOOD_LISTSTATS = ".blood liststats";
        public const string BCCOM_BLOOD_CHOOSESTAT = ".blood choosestat {0} {1}";
        
        // Sign Commands
        public const string BCCOM_SIGN_CREATE = ".sign create {0} {1} {2}"; // text, hexcolor, size
        public const string BCCOM_SIGN_REMOVE = ".sign remove 2";
        
        // Pond Command
        public const string BCCOM_POND = ".pond";
        
        // User Commands
        public const string BCCOM_USER_STATS = ".misc userstats";
        public const string BCCOM_RESET_MUSIC = ".misc silence";
        public const string BCCOM_QUEST_PREPARE = ".misc prepare";
        public const string BCCOM_STARTER_KIT = ".misc kitme";
        
        // Admin Commands - Player Management
        public const string BCCOM_ADMIN_GOD = ".god {0}";
        public const string BCCOM_ADMIN_MORTAL = ".mortal {0}";
        public const string BCCOM_ADMIN_TELEPORT = ".teleport {0} {1} {2} {3}";
        public const string BCCOM_ADMIN_REVIVE = ".revive {0}";
        public const string BCCOM_ADMIN_KILL = ".killplayer {0}";
        
        // Admin Commands - Server Management
        public const string BCCOM_ADMIN_SETTIME = ".settime {0} {1}";
        public const string BCCOM_ADMIN_REGION_LOCK = ".region lock {0}";
        public const string BCCOM_ADMIN_REGION_UNLOCK = ".region unlock {0}";
        
        // Admin Commands - Item/Resource
        public const string BCCOM_ADMIN_GIVE = ".give {0} {1}";
        public const string BCCOM_ADMIN_SPAWN_NPC = ".spawnnpc {0} {1} {2}";

        private enum InterceptFlag
        {
            ListBoxes,
            ListBoxContent,
            FamStats
        }

        private static readonly Dictionary<InterceptFlag, int> Flags = new();
        const string COLOR_PATTERN = "<color=.*?>(.*?)</color>";
        const string EXTRACT_BOX_NAME_PATTERN = "<color=[^>]+>(?<box>.*?)</color>";
        const string EXTRACT_COLOR_PATTERN = "(?<=<color=)[^>]+";
        const string EXTRACT_FAM_LVL_PATTERN = @"\[<color=[^>]+>(\d+)</color>\]\[<color=[^>]+>(\d+)</color>\].*?<color=yellow>(\d+)</color>.*?<color=white>(\d+)%</color>";
        const string EXTRACT_FAM_STATS_PATTERN = @"<color=[^>]+>([^<]+)</color>:\s*<color=[^>]+>([^<]+)</color>(?:,\s*)?";
        const string EXTRACT_FAM_NAME_PATTERN = @"<color=[^>]+>(?<name>[^<]+)</color>";
        const string EXTRACT_FAM_SCHOOL_PATTERN = @"-\s*<color=[^>]+>(?<school>[^<]+)</color>";

        private static string _currentBox;
        private static FamStats _currentFamStats;
        public static bool BoxContentFlag { get; set; }

        internal static void HandleMessage(Entity entity)
        {
            var chatMessage = entity.Read<ChatMessageServerEvent>();
            var message = chatMessage.MessageText.Value;

            if (chatMessage.MessageType == ServerChatMessageType.Local)
            {
                if (message.StartsWith(".fam"))
                {
                    switch (message)
                    {
                        case not null when message.StartsWith(".fam b"):
                        case not null when message.StartsWith(".familiar bind"):
                            Settings.LastBindCommand = message;
                            break;
                        case not null when message.StartsWith(BCCOM_FAMSTATS):
                            ClearFlags();
                            Flags[InterceptFlag.FamStats] = 1;
                            break;
                        case not null when message.StartsWith(BCCOM_LISTBOXES1):
                            ClearFlags();
                            Flags[InterceptFlag.ListBoxes] = 1;
                            var panel = Plugin.UIManager.GetPanel<BoxListPanel>();
                            if (panel != null)
                            {
                                panel.Reset();
                            }
                            break;
                        case not null when message.StartsWith(BCCOM_BOXCONTENT):
                            ClearFlags();
                            Flags[InterceptFlag.ListBoxContent] = 1;
                            if (_currentBox != null)
                            {
                                var boxPanel = Plugin.UIManager.GetBoxPanel(_currentBox);
                                if (boxPanel != null)
                                {
                                    boxPanel.Reset();
                                    ProcessBoxContentEntry(message);
                                }
                            }
                            break;
                    }


                    if (Settings.ClearServerMessages)
                        DestroyMessage(entity);
                    return;
                }
            }

            if (!chatMessage.MessageType.Equals(ServerChatMessageType.System))
                return;

            switch (message)
            {
                /////// FLAGS
                case not null when message.StartsWith("Couldn't find familiar to unbind"):
                    if (Settings.ClearServerMessages)
                        DestroyMessage(entity);
                    break;
                case not null when message.Contains(">unbound</color>!"):
                    break;
                case not null when message.Contains(">bound</color>!"):
                {
                    //old version failsafe
                    var regex =
                        new Regex(@"<color=\w+>(?<name>[^<]+)</color>(?:\s*<color=(?<color>#[A-Fa-f0-9]{6})>\*</color>)?");

                    Match match = regex.Match(message);
                    if (match.Success)
                    {
                        _oldVersionName = match.Groups["name"].Value;
                        _oldVersionColor = match.Groups["color"].Success ? GameHelper.GetSchoolFromHexColor(match.Groups["color"].Value) : null;
                    }
                }
                    break;

                /////// CLEANUP
                case not null when message.StartsWith("Couldn't find active familiar"):
                    if (Settings.ClearServerMessages)
                        DestroyMessage(entity);
                    break;
                case not null when message.StartsWith("Your familiar is level"):
                    ClearFlags();
                    Flags[InterceptFlag.FamStats] = Settings.IsFamStatsPanelEnabled ? 1 : 0;
                    ProcessFamStatsData(message, 0);
                    if (Settings.ClearServerMessages)
                        DestroyMessage(entity);
                    break;
                case not null when message.StartsWith("Familiar Boxes"):
                    ClearFlags();
                    Flags[InterceptFlag.ListBoxes] = Settings.IsBoxPanelEnabled ? 1 : 0;
                    Plugin.UIManager.GetPanel<BoxListPanel>()?.Reset();

                    if (Settings.ClearServerMessages)
                        DestroyMessage(entity);
                    break;
                case not null when message.StartsWith("<color=yellow>1</color>|"):
                    if (!BoxContentFlag) break;
                    BoxContentFlag = false;
                    ClearFlags();
                    Flags[InterceptFlag.ListBoxContent] = Settings.IsBoxPanelEnabled ? 1 : 0;
                    if (_currentBox != null)
                    {
                        var panel = Plugin.UIManager.GetBoxPanel(_currentBox);
                        if (panel != null)
                        {
                            panel.Reset();
                            ProcessBoxContentEntry(message);
                        }
                    }
                    if (Settings.ClearServerMessages)
                        DestroyMessage(entity);
                    break;
                case not null when message.StartsWith("Box Selected"):
                    var index = message.IndexOf('-');
                    var boxNameTemp = message.Substring(index, message.Length - index).Trim();
                    _currentBox = Regex.Matches(boxNameTemp, COLOR_PATTERN).FirstOrDefault()?.Groups[1].Value;
                    break;
                case not null when message.StartsWith("Emote actions <color=red>disabled</color>"):
                    if(_famEquipSequenceActive)
                        EnqueueMessage(BCCOM_ENABLEEQUIP);
                    break;
                case not null when message.StartsWith("Emote actions <color=green>enabled</color>"):
                    if (_famEquipSequenceActive)
                        FinishAutoEnableFamiliarEquipmentSequence();
                    break;

                default:
                    {
                        //fam stats
                        if (Flags.HasKeyValue(InterceptFlag.FamStats, 1))
                        {
                            if (message.StartsWith("Your familiar is level"))
                            {
                                ProcessFamStatsData(message, 0);
                            }
                            else if (message.StartsWith("<color=#00FFFF>MaxHealth"))
                            {
                                ProcessFamStatsData(message, 1);
                            }
                            else if (message.StartsWith("<color=green>"))
                            {
                                ProcessFamStatsData(message, 2);
                            }
                            DestroyMessage(entity);
                        }

                        //list box content
                        if (Flags.HasKeyValue(InterceptFlag.ListBoxContent, 1))
                        {
                            //stop
                            if (message.Length >= 2 && !message.Contains("</color>|"))
                            {
                                Flags.SetValue(InterceptFlag.ListBoxContent, 0);
                                return;
                            }

                            ProcessBoxContentEntry(message);
                            DestroyMessage(entity);
                        }

                        //list boxes
                        if (Flags.HasKeyValue(InterceptFlag.ListBoxes, 1))
                        {
                            //stop
                            if (!message.StartsWith("<color"))
                            {
                                Flags.SetValue(InterceptFlag.ListBoxes, 0);
                                return;
                            }
                            Regex regex = new Regex(EXTRACT_BOX_NAME_PATTERN);
                            MatchCollection matches = regex.Matches(message);

                            foreach (Match match in matches)
                            {
                                var text = match.Groups["box"].Value;
                                if (!string.IsNullOrEmpty(text))
                                {
                                    Plugin.UIManager.GetPanel<BoxListPanel>()?.AddListEntry(text);
                                }
                            }

                            DestroyMessage(entity);
                        }
                    }
                    break;
            }
        }

        private static void UpdateFamStatsUI()
        {
            Plugin.UIManager.GetPanel<FamStatsPanel>()?.UpdateData(_currentFamStats);
        }

        private static void ProcessFamStatsData(string message, int type)
        {
            switch (type)
            {
                case 0: //level data
                {
                    _currentFamStats = new FamStats();
                    //old version failsafe
                    _currentFamStats.Name = _oldVersionName;
                    _currentFamStats.School = _oldVersionColor?.ToString();

                    var match = Regex.Match(message, EXTRACT_FAM_LVL_PATTERN);
                    if (match.Success)
                    {
                        _currentFamStats.Level = int.Parse(match.Groups[1].Value);
                        _currentFamStats.PrestigeLevel = int.Parse(match.Groups[2].Value);
                        _currentFamStats.ExperienceValue = int.Parse(match.Groups[3].Value);
                        _currentFamStats.ExperiencePercent = int.Parse(match.Groups[4].Value);
                    }
                }
                    break;
                case 1:
                    {
                        var matches = Regex.Matches(message, EXTRACT_FAM_STATS_PATTERN);

                        foreach (Match match in matches)
                        {
                            if (match.Success)
                            {
                                string propName = match.Groups[1].Value.Trim();
                                string value = match.Groups[2].Value;
                                switch (propName)
                                {
                                    case "MaxHealth":
                                        _currentFamStats.MaxHealth = value;
                                        break;
                                    case "PhysicalPower":
                                        _currentFamStats.PhysicalPower = value;
                                        break;
                                    case "SpellPower":
                                        _currentFamStats.SpellPower = value;
                                        break;
                                    default:
                                        _currentFamStats.Stats.Add(propName, value);
                                        break;
                                }
                            }
                        }

                        ClearFlags();
                        UpdateFamStatsUI();
                    }
                    break;
                case 2: //name
                {
                    //old version fail safe
                    if(message.StartsWith("<color=green>Familiar Stats"))
                        break;
                    var nameMatch = Regex.Match(message, EXTRACT_FAM_NAME_PATTERN);
                    if (nameMatch.Success)
                    {
                        _currentFamStats.Name = nameMatch.Groups["name"].Value;
                    }
                    var schoolMatch = Regex.Match(message, EXTRACT_FAM_SCHOOL_PATTERN);
                    if (schoolMatch.Success)
                    {
                        _currentFamStats.School = schoolMatch.Groups["school"].Value;
                    }
                    else _currentFamStats.School = null;
                }
                    break;

            }
        }

        private static void ProcessBoxContentEntry(string message)
        {
            try
            {
                var colorText = Regex.Match(message, EXTRACT_COLOR_PATTERN).Value;
                var text = string.Join(' ', Regex.Matches(message, COLOR_PATTERN).Select(a => a.Groups[1].Value));
                var spellSchool = GameHelper.GetSchoolFromHexColor(colorText);
                var colorName = GameHelper.GetColorNameFromSchool(spellSchool);
                var text2 = $"{text.Substring(2).Trim()}{(spellSchool == null ? null : $" - {colorName.Name}")}";
                var number = Convert.ToInt32(text.Substring(0, char.IsDigit(text[1]) ? 2 : 1));
                Plugin.UIManager.GetBoxPanel(_currentBox)?.AddListEntry(number, text2, spellSchool);
            }
            catch
            {
                LogUtils.LogError($"{nameof(ProcessBoxContentEntry)} parsing error");
            }
        }

        private static void ClearFlags()
        {
            Flags.Clear();
        }

        private static void DestroyMessage(Entity entity)
        {
            if (Settings.ClearServerMessages)
                Plugin.EntityManager.DestroyEntity(entity);
        }
    }

    public class FamStats
    {
        public int Level { get; set; }
        public int PrestigeLevel { get; set; }
        public int ExperienceValue { get; set; }
        public int ExperiencePercent { get; set; }
        public string MaxHealth { get; set; }
        public string PhysicalPower { get; set; }
        public string SpellPower { get; set; }
        public string Name { get; set; }
        public string School { get; set; }
        public Dictionary<string, string> Stats { get; set; } = new();

        public string CurrentHealth { get; set; }
    }
}
