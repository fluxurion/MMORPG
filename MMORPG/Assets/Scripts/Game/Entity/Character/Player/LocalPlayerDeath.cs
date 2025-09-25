using System.Collections;
using MMORPG.Common.Proto.Base;
using MMORPG.Common.Proto.Player;
using MMORPG.Event;
using MMORPG.System;
using MMORPG.Tool;
using QFramework;
using Serilog;
using UnityEngine;
using NotImplementedException = System.NotImplementedException;

namespace MMORPG.Game
{
    public class LocalPlayerDeath : LocalPlayerAbility, IController, ICanSendEvent
    {
        private INetworkSystem _network;

        public override void OnStateInit()
        {
            _network = this.GetSystem<INetworkSystem>();
        }

        public override void OnStateEnter()
        {
            OwnerState.Brain.ActorController.Rigidbody.isKinematic = true;
            OwnerState.Brain.ActorController.Animator.SetTrigger("Death");
            StartCoroutine("ReviveCo");
            this.SendEvent(new MinePlayerDeathEvent() { Player = OwnerState.Brain });
        }

        public override void OnStateExit()
        {
            StopCoroutine("ReviveCo");
            OwnerState.Brain.ActorController.Animator.Play("Idle");
            OwnerState.Brain.ActorController.Rigidbody.isKinematic = false;
            this.SendEvent(new MinePlayerReviveEvent() { Player = OwnerState.Brain });
        }

        private IEnumerator ReviveCo()
        {
            yield return new WaitForSeconds(OwnerState.Brain.ReviveTime);
            Revive();
        }

        private async void Revive()
        {
            _network.SendToServer(new ReviveRequest());

            var response = await _network.ReceiveAsync<ReviveResponse>();
            if (response.Error != NetError.Success)
            {
                Log.Error($"Resurrection failed, reasons:{response.Error}");
                return;
            }
            Log.Information("Resurrection successful");
            OwnerState.Brain.ChangeStateByName("Idle");
        }

        public IArchitecture GetArchitecture()
        {
            return GameApp.Interface;
        }
    }
}
