﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameServer.Manager;
using GameServer.PlayerSystem;
using MMORPG.Common.Proto.Inventory;
using MMORPG.Common.Proto.Npc;
using Serilog;

namespace GameServer.NpcSystem
{
    public class DialogueManager
    {
        public Player PlayerOwner;
        private Dictionary<int, DialogueRecord> _recordDict = new();        // key:NpcId
        private bool _hasChange = true;
        private DialogueInfo? _dialogueInfo;
        public DialogueManager(Player playerOwner)
        {
            PlayerOwner = playerOwner;
        }

        public DialogueInfo GetDialogueInfo()
        {
            if (_hasChange || _dialogueInfo == null)
            {
                _dialogueInfo = new();
                _dialogueInfo.DialogueArr.AddRange(_recordDict.Select(x => x.Value));
                _hasChange = false;
            }
            return _dialogueInfo;
        }

        public void LoadDialogueInfo(byte[]? dialogueInfoData)
        {
            if (dialogueInfoData == null)
            {
                return;
            }
            DialogueInfo info = DialogueInfo.Parser.ParseFrom(dialogueInfoData);
            foreach (var record in info.DialogueArr)
            {
                if (!DataManager.Instance.NpcDict.TryGetValue(record.NpcId, out var define))
                {
                    Log.Error($"NpcId does not exist:{record.NpcId}");
                    continue;
                }
                _recordDict[record.NpcId] = record;
            }
        }

        public int GetDialogueId(int npcId)
        {
            if (_recordDict.TryGetValue(npcId, out var record))
            {
                return record.DialogueId;
            }
            if (!DataManager.Instance.NpcDict.TryGetValue(npcId, out var define))
            {
                Log.Error($"NpcId does not exist:{npcId}");
                return 0;
            }
            return define.StartDialogueId;
        }

        public void SaveDialogueId(int npcId, int dialogueId)
        {
            _hasChange = true;
            if (_recordDict.TryGetValue(npcId, out var record))
            {
                _recordDict[npcId].DialogueId = dialogueId;
                return;
            }
            _recordDict[npcId] = new()
            {
                NpcId = npcId,
                DialogueId = dialogueId,
            };
        }
    }
}
