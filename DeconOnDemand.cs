using Smod2;
using Smod2.Attributes;
using Smod2.EventHandlers;
using Smod2.Events;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace ArithFeather.DeconOnDemand
{
	[PluginDetails(
		author = "Arith",
		name = "Decon On Demand",
		description = "",
		id = "ArithFeather.DeconOnDemand",
		configPrefix = "afdod",
		version = "1.1",
		SmodMajor = 3,
		SmodMinor = 4,
		SmodRevision = 0
		)]
	public class DeconOnDemand : Plugin, IEventHandlerCallCommand, IEventHandlerWaitingForPlayers
	{
		private const string InfoMessage = "'.decon disable' - Disables decontamination)\n" +
							"'.decon #'  Starts decontamination with minute value\n" +
							"Announcements in:\n" +
							"15 Minutes.\n" +
							"10 Minutes.\n" +
							"5 Minutes.\n" +
							"1 Minute.\n" +
							"0.5 Minutes. (30 seconds)\n";

		public override void OnDisable() => Info("DeconOnDemand disabled.");
		public override void OnEnable() => Info("DeconOnDemand enabled.");
		public override void Register()
		{
			instance = this;
			AddEventHandlers(this);
		}

		private static DeconOnDemand instance;
		private DecontaminationLCZ decontamination;

		public static void StartDecontamination(float timeInMinutes) => instance.InitializeDecontamination(timeInMinutes);
		public static void StartDecontamination(int announcement) => instance.InitializeDecontamination(announcement);
		public static void DisableDecontamination() => instance.disableDeconInfo.SetValue(instance.decontamination, true);


		private readonly FieldInfo curAnmInfo = typeof(DecontaminationLCZ).GetField("curAnm", BindingFlags.NonPublic | BindingFlags.Instance);
		private readonly FieldInfo disableDeconInfo = typeof(DecontaminationLCZ).GetField("smDisableDecontamination", BindingFlags.NonPublic | BindingFlags.Instance);

		public void OnWaitingForPlayers(WaitingForPlayersEvent ev)
		{
			decontamination = GameObject.Find("Host").GetComponent<DecontaminationLCZ>() as DecontaminationLCZ;
		}

		private void InitializeDecontamination(float minutes)
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
		private void InitializeDecontamination(int announcement)
		{
			var anns = decontamination.announcements;

			if (announcement >= 0 && announcement <= 5)
			{
				disableDeconInfo.SetValue(decontamination, false);
				curAnmInfo.SetValue(decontamination, announcement);
				decontamination.time = anns[announcement].startTime * 60f;
			}
		}

		public void OnCallCommand(PlayerCallCommandEvent ev)
		{
			var rank = ev.Player.GetRankName().ToUpper();
			if (rank == "OWNER" || rank == "MOD" || rank == "ADMIN")
			{
				string[] inputs = ev.Command.Split(' ');

				if (inputs.Count() > 1 && inputs[0].ToUpper() == "DECON")
				{
					var input = inputs[1].ToUpper();

					if (input == "HELP")
					{
						ev.ReturnMessage = InfoMessage;
					}
					else if (input == "DISABLE")
					{
						disableDeconInfo.SetValue(decontamination, true);
						ev.ReturnMessage = "Decontamination Disabled. Note you can not disable decontamination after decontaminate starts.";
					}
					else if (float.TryParse(inputs[1], out float minutes) && minutes >= 0)
					{
						InitializeDecontamination((float)minutes);
						ev.ReturnMessage = $"Initializing Decontamination.";
					}
					else
					{
						ev.ReturnMessage = $"Wrong Input..\n{InfoMessage}";
					}
				}
				else if (inputs.Count() == 1 && inputs[0].ToUpper() == "DECON")
				{
					ev.ReturnMessage = $"Wrong Input..\n{InfoMessage}";
				}
			}
		}
	}
}
