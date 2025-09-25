using MMORPG.Common.Network;
using MMORPG.Common.Proto.Base;
using MMORPG.Common.Proto.Character;
using MMORPG.Common.Proto.Player;
using GameServer.Db;
using GameServer.Network;
using GameServer.Tool;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using GameServer.EntitySystem;
using GameServer.Manager;
using MMORPG.Common.Proto.Inventory;
using MMORPG.Common.Proto.Entity;
using GameServer.MapSystem;
using MMORPG.Common.Proto.Npc;
using MMORPG.Common.Tool;
using Newtonsoft.Json;
using GameServer.NpcSystem;

namespace GameServer.NetService
{
    public class NpcService : ServiceBase<NpcService>
    {
        public void OnConnect(NetChannel sender)
        {
        }

        public void OnChannelClosed(NetChannel sender)
        {
        }

        public void OnHandle(NetChannel sender, InteractRequest req)
        {
            UpdateManager.Instance.AddTask(() =>
            {
                if (sender.User == null || sender.User.Player == null) return;
                var player = sender.User.Player;
                var npc = player.InteractingNpc;
                var res = new InteractResponse()
                {
                    Error = NetError.InvalidEntity,
                };
                if (npc == null)
                {
                    // Find the nearest NPC
                    var entity =
                        player.Map.GetEntityFollowingNearest(player, entity => entity.EntityType == EntityType.Npc);
                    do
                    {
                        if (entity == null) break;
                        npc = entity as NpcSystem.Npc;
                        if (npc == null) break;
                        var distance = Vector2.Distance(player.Position, npc.Position);
                        if (distance > 1) break;
                        res.Error = NetError.Success;
                    } while (false);

                    if (res.Error != NetError.Success)
                    {
                        sender.Send(res, null);
                        return;
                    }

                    player.InteractingNpc = npc;
                    player.CurrentDialogueId = player.DialogueManager.GetDialogueId(npc.NpcDefine.ID);
                }
                else
                {
                    res.Error = NetError.Success;
                }

                if (player.CurrentDialogueId == 0)
                {
                    // need to end conversation
                    res.DialogueId = 0;
                    player.InteractingNpc = null;
                    sender.Send(res, null);
                    return;
                }

                res.EntityId = npc.EntityId;

                var dialogueDefine = DataManager.Instance.DialogueDict[player.CurrentDialogueId];
                if (req.SelectIdx != 0)
                {
                    var options = DataHelper.ParseIntegers(dialogueDefine.Options);

                    if (req.SelectIdx < 0 || req.SelectIdx > options.Length)
                    {
                        Log.Error("The client passed in an incorrect dialogue selection index.");
                        return;
                    }

                    // Select an item and inform the client of the jump to that item
                    dialogueDefine = DataManager.Instance.DialogueDict[options[req.SelectIdx - 1]];
                    player.CurrentDialogueId = dialogueDefine.Jump;
                    if (player.CurrentDialogueId == 0)
                    {
                        // There is no dialog to jump to, end
                        res.DialogueId = 0;
                        player.InteractingNpc = null;
                        sender.Send(res, null);
                        return;
                    }
                }

                res.DialogueId = player.CurrentDialogueId;

                bool next = true;
                if (dialogueDefine.AcceptTask != "")
                {
                    // Take mission
                    var tmp = JsonConvert.DeserializeObject<int[]>(dialogueDefine.AcceptTask);
                    if (!player.TaskManager.AcceptTask(tmp[0]))
                    {
                        player.CurrentDialogueId = 0;
                        res.DialogueId = tmp[1];
                        next = false;
                    }
                }
                
                if (dialogueDefine.SubmitTask != "")
                {
                    // Submit task
                    var tmp = JsonConvert.DeserializeObject<int[]>(dialogueDefine.SubmitTask);
                    if (!player.TaskManager.SubmitTask(tmp[0]))
                    {
                        player.CurrentDialogueId = 0;
                        res.DialogueId = tmp[1];
                        next = false;
                    }
                }

                if (req.SelectIdx != 0 && player.CurrentDialogueId != 0)
                {
                    dialogueDefine = DataManager.Instance.DialogueDict[player.CurrentDialogueId];
                }
                // If you need to save the current conversation progress
                if (dialogueDefine.SaveDialogueId != 0)
                {
                    player.DialogueManager.SaveDialogueId(npc.NpcDefine.ID, dialogueDefine.SaveDialogueId);
                }

                if (next)
                {
                    // Go to the next conversation
                    player.CurrentDialogueId = dialogueDefine.Jump;
                }


                if (player.CurrentDialogueId == 0)
                {
                    if (dialogueDefine.Options.Any())
                    {
                        // It is a selection dialog, waiting for selection
                        player.CurrentDialogueId = res.DialogueId;
                    }
                    else
                    {
                        // Need to end the conversation and wait for the next stop response
                        // player.InteractingNpc = null;
                        player.CurrentDialogueId = 0;
                    }
                }

                sender.Send(res, null);
            });
        }

        public void OnHandle(NetChannel sender, QueryDialogueIdRequest req)
        {
            UpdateManager.Instance.AddTask(() =>
            {
                if (sender.User == null || sender.User.Player == null) return;
                var player = sender.User.Player;

                var e = EntityManager.Instance.GetEntity(req.EntityId);
                var res = new QueryDialogueIdResponse()
                {
                    Error = NetError.Success,
                };
                if (e == null || !(e is Npc))
                {
                    res.Error = NetError.InvalidEntity;
                }
                else
                {
                    var npc = (Npc)e;
                    res.EntityId = e.EntityId;
                    res.DialogueId = player.DialogueManager.GetDialogueId(npc.NpcDefine.ID);
                }
                sender.Send(res, null);
            });
        }
    }
}
