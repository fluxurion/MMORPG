using QFramework;
using MMORPG.Tool;
using Serilog;
using UnityEngine;

namespace MMORPG.Game
{
    public class InitToolState : AbstractState<LaunchStatus, LaunchController>, IController
    {
        public InitToolState(FSM<LaunchStatus> fsm, LaunchController target) : base(fsm, target)
        {
        }

        protected override void OnEnter()
        {
            Log.Information("Initialization tool");
            new GameObject(nameof(UIToolController)).AddComponent<UIToolController>();
            mFSM.ChangeState(LaunchStatus.InitNetwork);
        }

        public IArchitecture GetArchitecture()
        {
            return GameApp.Interface;
        }
    }
}
