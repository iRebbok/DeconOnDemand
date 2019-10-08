using Smod2;
using Smod2.API;
using Smod2.Attributes;
using Smod2.Commands;
using Smod2.Config;
using Smod2.EventHandlers;
using Smod2.Events;
using System.Collections.Generic;
using System.Reflection;

namespace ArithFeather.DeconOnDemand
{
    [PluginDetails(
        author = "Arith",
        name = "Decon On Demand",
        description = "",
        id = "ArithFeather.DeconOnDemand",
        // configPrefix = "afdod",
        version = "1.3.1",
        SmodMajor = 3,
        SmodMinor = 5,
        SmodRevision = 0
        )]
    public class DeconOnDemand : Plugin
    {
        private static readonly string infoMessage = string.Join("\n", new string[] { "", "Announcements in:", " - '15' Minutes", " - '10' Minutes", " - '5' Minutes", " - '1' Minutes", " - '0.5' Minutes (30 secound)" });

        private static DeconOnDemand instance;

        public override void OnDisable() { this.Info("DeconOnDemand disabled."); }
        public override void OnEnable() { instance = this; this.Info("DeconOnDemand enabled."); }
        public override void Register()
        {
            this.AddConfig(new ConfigSetting("afgod_whitelist", new string[] { "admin", "owner" }, true, "Allowed ranks to use commands"));
            this.AddConfig(new ConfigSetting("afgod_disable", false, true, ""));

            this.AddCommand("afdod_info", new InfoCommandHandler());
            this.AddCommand("afdod_decon", new DeconCommandHandler());
        }

        public class EventHandler : IEventHandlerWaitingForPlayers
        {
            private DeconOnDemand plugin => DeconOnDemand.instance;
            internal static List<string> allowedRanks;

            public void OnWaitingForPlayers(WaitingForPlayersEvent ev)
            {
                if (plugin.GetConfigBool("afgod_disable")) PluginManager.Manager.DisablePlugin(plugin);

                allowedRanks = new List<string>();
                string[] ranks = plugin.GetConfigList("afgod_whitelist");
                foreach (string rank in ranks) if (!allowedRanks.Contains(rank)) allowedRanks.Add(rank);

                LogicManager.decontamination = PlayerManager.localPlayer.GetComponent<DecontaminationLCZ>();
            }
        }

        public class InfoCommandHandler : ICommandHandler
        {
            public string GetCommandDescription() => "Information of decon announcements";
            public string GetUsage() => "afdod_info";
            public string[] OnCall(ICommandSender sender, string[] args) => new string[] { infoMessage };
        }

        public class DeconCommandHandler : ICommandHandler
        {
            public string GetCommandDescription() => "";
            public string GetUsage() => "afdod_decon <time/disable>";

            public string[] OnCall(ICommandSender sender, string[] args)
            {
                if (sender is Player p) if (!EventHandler.allowedRanks.Contains(p.GetRankName())) return new string[] { "Your rank is not in the list of allowed ranks, you cannot execute this command" };

                if (args.Length > 0)
                {
                    string arg = args[0];

                    if (arg.ToLower() == "disable")
                    {
                        LogicManager.disableDeconInfo.SetValue(LogicManager.decontamination, true);
                        return new string[] { "Decontamination disabled. Note you cannot disable decontamination after decontaminate starts" };
                    }

                    if (float.TryParse(arg, out float minutes))
                    {
                        LogicManager.InitializeDecontamination((float)minutes);
                        return new string[] { "Initializing decontamination" };
                    }
                }

                return new string[] { GetUsage() };
            }
        }

        public class LogicManager
        {
            internal static DecontaminationLCZ decontamination;

            public static void StartDecontamination(float timeInMinutes) => InitializeDecontamination(timeInMinutes);
            public static void StartDecontamination(int announcement) => InitializeDecontamination(announcement);
            public static void DisableDecontamination() => disableDeconInfo.SetValue(decontamination, true);

            internal static readonly FieldInfo curAnmInfo = typeof(DecontaminationLCZ).GetField("curAnm", BindingFlags.NonPublic | BindingFlags.Instance);
            internal static readonly FieldInfo disableDeconInfo = typeof(DecontaminationLCZ).GetField("smDisableDecontamination", BindingFlags.NonPublic | BindingFlags.Instance);

            internal static void InitializeDecontamination(float minutes)
            {
                if (minutes >= 0f)
                {
                    switch (minutes)
                    {
                        case 15f:
                            decontamination.time = 41f;
                            break;
                        case 10f:
                            decontamination.time = 239f;
                            break;
                        case 5f:
                            decontamination.time = 437f;
                            break;
                        case 1f:
                            decontamination.time = 635f;
                            break;
                        case 0.5f:
                            decontamination.time = 665f;
                            break;
                        case 0f:
                            decontamination.time = 703.4f;
                            break;
                        default:
                            decontamination.time = (11.74f - minutes) * 60;
                            break;
                    }

                    disableDeconInfo.SetValue(decontamination, false);

                    for (int i = 0; i < decontamination.announcements.Count; i++)
                    {
                        var annStartTime = decontamination.announcements[i].startTime;
                        if (decontamination.time / 60f < annStartTime)
                        {
                            curAnmInfo.SetValue(decontamination, i);
                            return;
                        }
                    }
                }
            }

            /// <summary>
            /// 0 - 15 Minutes - 0.7
            /// 1 - 10 Minutes - 4
            /// 2 - 5 Minutes - 7.3
            /// 3 - 1 Minute - 10.6
            /// 4 - 30 Seconds - 11.1
            /// 5 - Decontaminate - 11.74
            /// </summary>
            private static void InitializeDecontamination(int announcement)
            {
                var anns = decontamination.announcements;

                if (announcement >= 0 && announcement <= 5)
                {
                    disableDeconInfo.SetValue(decontamination, false);
                    curAnmInfo.SetValue(decontamination, announcement);
                    decontamination.time = anns[announcement].startTime * 60f;
                }
            }
        }
    }
}