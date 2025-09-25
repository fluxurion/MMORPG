using MMORPG.Command;
using MMORPG.Common.Proto.Fight;
using MMORPG.Common.Proto.Entity;
using MMORPG.Event;
using MMORPG.Tool;
using MMORPG.UI;
using QFramework;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using AnimationState = MMORPG.Common.Proto.Entity.AnimationState;
using MMORPG.Common.Proto.Base;
using System.Collections.Generic;
using Serilog;

namespace MMORPG.Game
{
    public class NpcBrain : MonoBehaviour, IController
    {
        public float GroundClearance;
        public ActorController ActorController;
        public FSM<AnimationState> FSM = new ();

        private GameObject _tip;

        private static Dictionary<string, GameObject> _tipDict;

        private void Awake()
        {

        }

        private void Start()
        {
            if (_tipDict == null)
            {
                _tipDict = new();
                _tipDict["doubt"] = Resources.Load<GameObject>("Prefabs/Effect/Npc/IconWhiteQuestion");
                _tipDict["sigh"] = Resources.Load<GameObject>("Prefabs/Effect/Npc/IconWhiteExclamation");
                _tipDict["asterisk"] = Resources.Load<GameObject>("Prefabs/Effect/Npc/IconWhiteReward");
            }

            this.RegisterEvent<InteractEvent>(e =>
            {
                if (e.Resp.Error != NetError.Success) return;
                if (e.Resp.DialogueId == 0)
                {
                    this.SendCommand(new QueryDialogueIdCommand(ActorController.Entity.EntityId));
                }
            }).UnRegisterWhenGameObjectDestroyed(gameObject);

            this.SendCommand(new QueryDialogueIdCommand(ActorController.Entity.EntityId));

            this.RegisterEvent<QueryDialogueIdEvent>(e =>
            {
                if (e.Resp.Error != NetError.Success) return;
                if (e.Resp.DialogueId != 0)
                {
                    LoadTip(e.Resp.DialogueId);
                }
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
        }

        private void LoadTip(int dialogueId)
        {
            // Mount overhead special effects
            if (_tip != null)
            {
                Destroy(_tip);
                _tip = null;
            }
            var dataManagerSystem = this.GetSystem<IDataManagerSystem>();
            var dialogueDefine = dataManagerSystem.GetDialogueDefine(dialogueId);
            if (dialogueDefine.TipResource == "")
            {
                if (_tip != null)  Destroy(_tip);
                return;
            }
            Log.Information($"{dialogueDefine.TipResource} - {_tipDict[dialogueDefine.TipResource]}");
            _tip = Instantiate(_tipDict[dialogueDefine.TipResource], transform);

            // Get CapsuleCollider to confirm NPC height
            CapsuleCollider capsuleCollider = GetComponent<CapsuleCollider>();
            if (capsuleCollider != null)
            {
                float npcHeight = capsuleCollider.height;
                _tip.transform.localPosition = new Vector3(0, npcHeight + 0.8f, 0);
            }
            else
            {
                // if not CapsuleCollider，You can set a default height or other processing methods
                _tip.transform.localPosition = new Vector3(0, 2, 0);
            }

            _tip.transform.localScale = new Vector3(2, 2, 2);
        }

        private void Update()
        {
            FSM.Update();
        }

        private void FixedUpdate()
        {
            FSM.FixedUpdate();
        }

        private void OnGUI()
        {
            FSM.OnGUI();
        }

        private void OnDestroy()
        {
            FSM.Clear();
        }

        
        public IArchitecture GetArchitecture()
        {
            return GameApp.Interface;
        }
    }

}
