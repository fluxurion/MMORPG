using System;
using QFramework;
using Serilog;
using UnityEngine;

namespace MMORPG.Game
{
    public enum LaunchStatus
    {
        InitLog,
        InitPlugins,
        InitTool,
        InitNetwork,
        InLobby,
        Playing,
        ApplicationQuit
    }

    public class LaunchController : MonoBehaviour
    {
        public FSM<LaunchStatus> FSM = new();

        public static LaunchController Instance { get; private set; }

        private void Awake()
        {
            DontDestroyOnLoad(this);
            Instance = this;
        }

        private void Start()
        {
            FSM.AddState(LaunchStatus.InitLog, new InitLogState(FSM, this));
            FSM.AddState(LaunchStatus.InitPlugins, new InitPluginsState(FSM, this));
            FSM.AddState(LaunchStatus.InitTool, new InitToolState(FSM, this));
            FSM.AddState(LaunchStatus.InitNetwork, new InitNetworkState(FSM, this));
            FSM.AddState(LaunchStatus.InLobby, new InLobbyState(FSM, this));
            FSM.AddState(LaunchStatus.Playing, new PlayingState(FSM, this));
            FSM.AddState(LaunchStatus.ApplicationQuit, new ApplicationQuitState(FSM, this));

            FSM.StartState(LaunchStatus.InitLog);
        }

        protected void OnApplicationQuit()
        {
            try
            {
                FSM.ChangeState(LaunchStatus.ApplicationQuit);
            }
            catch (Exception e)
            {
                Log.Error(e, "Error exiting the program!");
            }
            finally
            {
                FSM.Clear();
                GameApp.Interface.Deinit();
                Log.CloseAndFlush();
            }
        }
    }
}
