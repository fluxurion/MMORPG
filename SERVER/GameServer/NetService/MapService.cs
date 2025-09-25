using System.Diagnostics;
using GameServer.Db;
using MMORPG.Common.Network;
using MMORPG.Common.Proto.Base;
using MMORPG.Common.Proto.Entity;
using GameServer.Network;
using MMORPG.Common.Proto.Map;
using Google.Protobuf.WellKnownTypes;
using Serilog;
using GameServer.Manager;
using GameServer.UserSystem;

namespace Service
{
    public class MapService : ServiceBase<MapService>
    {
        public void OnConnect(NetChannel sender)
        {
        }

        public void OnChannelClosed(NetChannel sender)
        {
            var player = sender.User?.Player;
            if (player == null) return;
            player.Map.PlayerManager.RemovePlayer(player);
            player.Map.EntityLeave(player);
        }

        public void OnHandle(NetChannel sender, EntityTransformSyncRequest request)
        {
            UpdateManager.Instance.AddTask(() =>
            {
                //Log.Debug($"{request.EntityId}Request synchronization: Pos:{request.Transform.Position} | Id:{request.StateId}");
                sender.User?.Player?.Map.EntityTransformSync(request.EntityId, request.Transform, request.StateId,
                    request.Data);
            });
        }


        public void OnHandle(NetChannel sender, SubmitChatMessageRequest request)
        {
            UpdateManager.Instance.AddTask(() =>
            {
                Log.Debug($"{sender}Send chat request: Message:{request.Message}, Type:{request.MessageType}");
                var time = Timestamp.FromDateTime(DateTime.UtcNow);

                sender.Send(new SubmitChatMessageResponse()
                {
                    Error = NetError.Success,
                    Timestamp = time
                });

                Debug.Assert(sender.User?.Player != null);

                // If it is the beginning of a cheat command and it is an administrator account
                if (request.Message.StartsWith("--/") && sender.User.DbUser.Authority == Authoritys.Administrator)
                {
                    var cmd = request.Message[3..];
                    var str = $"administrator:\"{sender}\"Use cheats:{cmd}";
                    if (cmd == "Level up")
                    {
                        sender.User?.Player?.AddLevel(1);
                        str += $", Level increased by 1, current level:{sender.User?.Player?.Level}";
                    }
                    Log.Information(str);
                }
                else
                {
                    sender.User?.Player?.Map.PlayerManager.Broadcast(new ReceiveChatMessageResponse()
                    {
                        CharacterId = sender.User.Player.DbCharacter.Id,
                        CharacterName = sender.User.Player.Name,
                        Message = request.Message,
                        MessageType = request.MessageType,
                        Timestamp = time
                    }, sender.User.Player, false);
                }
            });
        }
    }
}
