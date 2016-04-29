﻿#region Using

using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Networking;
using Sandbox.Engine.Utils;
using Sandbox.Game;
using Sandbox.Game.Gui;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using VRage.Game;
using VRage.Utils;
using VRage.Trace;
using VRage.Library.Utils;
using VRage.Network;


#endregion

namespace Sandbox.Engine.Multiplayer
{
    public class MyDedicatedServer : MyDedicatedServerBase
    {
        #region Fields

        float m_inventoryMultiplier;
        float m_assemblerMultiplier;
        float m_refineryMultiplier;
        float m_welderMultiplier;
        float m_grinderMultiplier;

        #endregion

        #region Properties

        public override float InventoryMultiplier
        {
            get { return m_inventoryMultiplier; }
            set { m_inventoryMultiplier = value; }
        }

        public override float AssemblerMultiplier
        {
            get { return m_assemblerMultiplier; }
            set { m_assemblerMultiplier = value; }
        }

        public override float RefineryMultiplier
        {
            get { return m_refineryMultiplier; }
            set { m_refineryMultiplier = value; }
        }

        public override float WelderMultiplier
        {
            get { return m_welderMultiplier; }
            set { m_welderMultiplier = value; }
        }

        public override float GrinderMultiplier
        {
            get { return m_grinderMultiplier; }
            set { m_grinderMultiplier = value; }
        }

        public override bool Scenario
        {
            get;
            set;
        }

        public override string ScenarioBriefing
        {
            get;
            set;
        }

        public override DateTime ScenarioStartTime
        {
            get;
            set;
        }

        public override bool Battle
        {
            get;
            set;
        }

        public override float BattleRemainingTime
        {
            get;
            set;
        }

        public override bool BattleCanBeJoined
        {
            get;
            set;
        }

        public override ulong BattleWorldWorkshopId
        {
            get;
            set;
        }

        public override int BattleFaction1MaxBlueprintPoints
        {
            get;
            set;
        }

        public override int BattleFaction2MaxBlueprintPoints
        {
            get;
            set;
        }

        public override int BattleFaction1BlueprintPoints
        {
            get;
            set;
        }

        public override int BattleFaction2BlueprintPoints
        {
            get;
            set;
        }

        public override int BattleMapAttackerSlotsCount
        {
            get;
            set;
        }

        public override long BattleFaction1Id
        {
            get;
            set;
        }

        public override long BattleFaction2Id
        {
            get;
            set;
        }

        public override int BattleFaction1Slot
        {
            get;
            set;
        }

        public override int BattleFaction2Slot
        {
            get;
            set;
        }

        public override bool BattleFaction1Ready
        {
            get;
            set;
        }

        public override bool BattleFaction2Ready
        {
            get;
            set;
        }

        public override int BattleTimeLimit
        {
            get;
            set;
        }

        #endregion

        #region Constructor

        internal MyDedicatedServer(IPEndPoint serverEndpoint)
            : base(new MySyncLayer(new MyTransportLayer(MyMultiplayer.GameEventChannel)))
        {
            Initialize(serverEndpoint);
        }

        #endregion

        internal override void SendGameTagsToSteam()
        {
            if (SteamSDK.SteamServerAPI.Instance != null)
            {
                var serverName = MySandboxGame.ConfigDedicated.ServerName.Replace(":", "a58").Replace(";", "a59");

                var gamemode = new StringBuilder();

                switch (GameMode)
                {
                    case MyGameModeEnum.Survival:
                        gamemode.Append(String.Format("S{0}-{1}-{2}", (int)InventoryMultiplier, (int)AssemblerMultiplier, (int)RefineryMultiplier));
                        break;
                    case MyGameModeEnum.Creative:
                        gamemode.Append("C");
                        break;

                    default:
                        Debug.Fail("Unknown game type");
                        break;
                }

                SteamSDK.SteamServerAPI.Instance.GameServer.SetGameTags(
                    "groupId" + m_groupId.ToString() +
                    " version" + MyFinalBuildConstants.APP_VERSION.ToString() +
                    " datahash" + MyDataIntegrityChecker.GetHashBase64() +
                    " " + MyMultiplayer.ModCountTag + ModCount +
                    " gamemode" + gamemode +
                    " " + MyMultiplayer.ViewDistanceTag + ViewDistance);
            }
        }

        protected override void SendServerData()
        {
            ServerDataMsg msg = new ServerDataMsg();
            msg.WorldName = m_worldName;
            msg.GameMode = m_gameMode;
            msg.InventoryMultiplier = m_inventoryMultiplier;
            msg.AssemblerMultiplier = m_assemblerMultiplier;
            msg.RefineryMultiplier = m_refineryMultiplier;
            msg.WelderMultiplier = m_welderMultiplier;
            msg.GrinderMultiplier = m_grinderMultiplier;
            msg.HostName = m_hostName;
            msg.WorldSize = m_worldSize;
            msg.AppVersion = m_appVersion;
            msg.MembersLimit = m_membersLimit;
            msg.DataHash = m_dataHash;

            ReplicationLayer.SendWorldData(ref msg);
        }

        protected override void OnChatMessage(ref ChatMsg msg)
        {
            bool debugCommands = !MyFinalBuildConstants.IS_OFFICIAL && MyFinalBuildConstants.IS_DEBUG;

            if (m_memberData.ContainsKey(msg.Author))
            {
                if (m_memberData[msg.Author].IsAdmin || debugCommands)
                {
                    if (msg.Text.Equals("+save", StringComparison.InvariantCultureIgnoreCase))
                    {
                        MySession.Static.Save();
                    }
                    else if (msg.Text.Contains("+unban", StringComparison.InvariantCultureIgnoreCase))
                    {
                        string[] parts = msg.Text.Split(' ');
                        if (parts.Length > 1)
                        {
                            ulong user = 0;
                            if (ulong.TryParse(parts[1], out user))
                            {
                                BanClient(user, false);
                            }
                        }
                    }
                }
                if(debugCommands)
                {
                    MyServerDebugCommands.Process(msg.Text, msg.Author);
                }
            }
            if(msg.Text.Contains("+43Dump"))
            {
                MySession.InitiateDump();
                return;
            }

            RaiseChatMessageReceived(msg.Author, msg.Text, ChatEntryTypeEnum.ChatMsg);
        }
    }
}
