using BepInEx;
using BepInEx.Configuration;
using Newtonsoft.Json;
using UnityEngine;

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Collections;
using System.Data;
using Bounce.Unmanaged;

namespace LordAshes
{
	[BepInPlugin(Guid, Name, Version)]
    [BepInDependency(ChatServicePlugin.Guid)]
	public partial class ChatRollPlugin : BaseUnityPlugin
	{
		// Plugin info
		public const string Name = "Chat Roll Plug-In";
		public const string Guid = "org.lordashes.plugins.chatroll";
		public const string Version = "1.0.0.0";

        /// <summary>
        /// Function for initializing plugin
        /// This function is called once by TaleSpire
        /// </summary>
        void Awake()
		{
			UnityEngine.Debug.Log("Chat Roll Plugin: Active.");

            bool whisperPluginAvailable = ChatServicePlugin.handlers.ContainsKey("/w ");

            ChatServicePlugin.handlers.Add("/r ", (roll, roller, source) =>
            {
                NGuid sender = CheckSingleSource(roller, source);
                if (sender!=NGuid.Empty)
                {
                    Dictionary<string,string> results = ResolveRoll(roll, roller);
                    string message = "<size=20>" + results["Roll"] + "= <size=20>(" + results["Expanded"]+") = <size=32>" + results["Total"];
                    ChatManager.SendChatMessage(message, sender);
                }
                return null;
            });

            ChatServicePlugin.handlers.Add("/rn ", (roll, roller, source) =>
            {
                roll = roll.Substring(roll.IndexOf(" ") + 1); // Ship /rn
                string name = roll.Substring(0, roll.IndexOf(" ")).Trim(); // Extract roll name
                roll = roll.Substring(roll.IndexOf(" ") + 1); // Skip roll name
                NGuid sender = CheckSingleSource(roller, source);
                if (sender != NGuid.Empty)
                {
                    Dictionary<string, string> results = ResolveRoll("/r " + roll, roller);
                    string message = "<size=32>" + name + ": <size=20>" + results["Roll"] + "= <size=20>(" + results["Expanded"] + ") = <size=32>" + results["Total"];
                    ChatManager.SendChatMessage(message, sender);
                }
                return null;
            });

            if (whisperPluginAvailable)
            {
                ChatServicePlugin.handlers.Add("/gr ", (roll, roller, source) =>
                {
                    NGuid sender = CheckSingleSource(roller, source);
                    if (sender != NGuid.Empty)
                    {
                        Dictionary<string, string> results = ResolveRoll(roll, roller);
                        string message = "/w " + CampaignSessionManager.GetPlayerName(FindGM()) + " <size=20>" + results["Roll"] + "= <size=20>(" + results["Expanded"] + ") = <size=32>" + results["Total"];
                        ChatManager.SendChatMessage(message, sender);
                    }
                    return null;
                });

                ChatServicePlugin.handlers.Add("/grn ", (roll, roller, source) =>
                {
                    roll = roll.Substring(roll.IndexOf(" ") + 1); // Ship /grn
                string name = roll.Substring(0, roll.IndexOf(" ")).Trim(); // Extract roll name
                roll = roll.Substring(roll.IndexOf(" ") + 1); // Skip roll name
                NGuid sender = CheckSingleSource(roller, source);
                    if (sender != NGuid.Empty)
                    {
                        Dictionary<string, string> results = ResolveRoll("/r " + roll, roller);
                        string message = "/w " + CampaignSessionManager.GetPlayerName(FindGM()) + " <size=32>" + name + ": <size=20>" + results["Roll"] + "= <size=20>(" + results["Expanded"] + ") = <size=32>" + results["Total"];
                        ChatManager.SendChatMessage(message, sender);
                    }
                    return null;
                });
            }
            else
            {
                Debug.LogWarning("Chat Roll Plugin: Missing Chat Whisper Plugin. Roll Modes /gr and /grn Will Not Be Available");
            }
        }

        /// <summary>
        /// Only the roller processes the roll and then rebroadcasts the results.
        /// This method check to see if the player or the player's mini is the roller.
        /// Returns an NGuid of the player or mini if yes. Returns Empty NGUid of not.
        /// </summary>
        /// <param name="roller">Name of the roller</param>
        /// <param name="source">Type of roll (gm, player or mini)</param>
        /// <returns></returns>
        private NGuid CheckSingleSource(string roller, ChatServicePlugin.ChatSource source)
        {
            NGuid sender = NGuid.Empty;
            if (roller == ".")
            {
                // Shortcut for self
                sender = new NGuid(LocalPlayer.Id.ToString());
            }
            else
            {
                switch (source)
                {
                    case ChatServicePlugin.ChatSource.player:
                    case ChatServicePlugin.ChatSource.gm:
                        // If source is player or GM then local player name must match the roller in order to process roll
                        if (CampaignSessionManager.GetPlayerName(LocalPlayer.Id) == roller)
                        {
                            sender = new NGuid(LocalPlayer.Id.ToString());
                        }
                        break;
                    case ChatServicePlugin.ChatSource.creature:
                        // If source is creature then the local player must have control of the roller in order to process roll
                        foreach (CreatureBoardAsset asset in CreaturePresenter.AllCreatureAssets)
                        {
                            if (asset.Creature.Name.StartsWith(roller))
                            {
                                if (LocalClient.CanControlCreature(asset.Creature.CreatureId))
                                {
                                    sender = new NGuid(asset.Creature.CreatureId.ToString());
                                }
                                break;
                            }
                        }
                        break;
                    default:
                        // Ignore anonymous rolls
                        break;
                }
            }
            return sender;
        }

        /// <summary>
        /// Method to process a roll. All dice are replaced with their corresponding rolls and the total is calculated.
        /// Returns a dictionary with Roll, Total and Expanded. Roll has the original roll. Total has the total.
        /// Expended has the original roll with all dice references replaced by the rolled numbers.
        /// </summary>
        /// <param name="roll">Formula of the roll</param>
        /// <param name="roller">Name of the roller</param>
        /// <returns>Dictionary with roll results</returns>
        private Dictionary<string,string> ResolveRoll(string roll, string roller)
        {
            try
            {
                Debug.Log("Roll: " + roll + " by " + roller);
                System.Random ran = new System.Random();
                roll = roll.Substring(roll.IndexOf(" ") + 1).Trim();
                roll = "0+" + roll + "+0";
                roll = roll.ToUpper();
                string originalRoll = roll;
                string expanded = originalRoll;
                while (roll.Contains("D"))
                {
                    int total = 0;
                    int pos = roll.IndexOf("D");
                    int sPos = pos - 1;
                    int ePos = pos + 1;
                    while ("0123456789".Contains(roll.Substring(sPos, 1))) { sPos--; if (sPos == 0) { break; } }
                    while ("0123456789".Contains(roll.Substring(ePos, 1))) { ePos++; if (ePos > roll.Length) { break; } }
                    int dice = int.Parse(roll.Substring(sPos + 1, pos - (sPos + 1)));
                    int sides = int.Parse(roll.Substring(pos + 1, ePos - (pos + 1)));
                    string rolls = "[";
                    for (int d = 0; d < dice; d++)
                    {
                        int pick = ran.Next(1, sides + 1);
                        rolls = rolls + pick + ",";
                        total = total + pick;
                    }
                    roll = roll.Substring(0, sPos + 1) + total + roll.Substring(ePos);
                    rolls = rolls.Substring(0, rolls.Length - 1) + "]";
                    int expPos = expanded.IndexOf(dice + "D" + sides);
                    expanded = expanded.Substring(0, expPos) + rolls + expanded.Substring(expPos + (dice.ToString() + "D" + sides.ToString()).Length);
                }
                DataTable dt = new DataTable();
                Dictionary<string, string> results = new Dictionary<string, string>();
                results.Add("Roll", originalRoll.Substring(2).Substring(0, originalRoll.Substring(2).Length - 2));
                results.Add("Total", dt.Compute(roll, null).ToString());
                results.Add("Expanded", expanded.Substring(2).Substring(0, expanded.Substring(2).Length - 2));
                return results;
            }
            catch(Exception e)
            {
                Dictionary<string, string> results = new Dictionary<string, string>();
                results.Add("Roll", roll.Substring(2).Substring(0, roll.Substring(2).Length - 2));
                results.Add("Total", roll.Substring(2).Substring(0, roll.Substring(2).Length - 2));
                results.Add("Expanded", e.Message);
                return results;
            }
        }

        private PlayerGuid FindGM()
        {
            foreach(KeyValuePair<PlayerGuid,PlayerInfo> player in CampaignSessionManager.PlayersInfo)
            {
                if (player.Value.Rights.CanGm) { return player.Key; }
            }
            return PlayerGuid.Empty;
        }
    }
}
