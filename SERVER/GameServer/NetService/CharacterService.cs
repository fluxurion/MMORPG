using MMORPG.Common.Network;
using MMORPG.Common.Proto.Base;
using MMORPG.Common.Proto.Character;
using GameServer.Db;
using GameServer.Network;
using GameServer.Tool;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameServer.MapSystem;
using GameServer.Manager;
using MMORPG.Common.Tool;

namespace GameServer.NetService
{
    public class CharacterService : ServiceBase<CharacterService>
    {
        private static readonly object _characterCreateLock = new();

        public void OnConnect(NetChannel sender)
        {
        }

        public void OnChannelClosed(NetChannel sender)
        {
        }


        public void OnHandle(NetChannel sender, CharacterCreateRequest request)
        {
            UpdateManager.Instance.AddTask(() =>
            {
                Log.Debug($"{sender}Role creation request");
                if (sender.User == null)
                {
                    Log.Debug($"{sender}Role creation failed: User not logged in");
                    return;
                }
                var count = SqlDb.FreeSql.Select<DbCharacter>()
                    .Where(t => t.UserId.Equals(sender.User.UserId))
                    .Count();
                if (count >= 4)
                {
                    sender.Send(new CharacterCreateResponse() { Error = NetError.CharacterCreationLimitReached });
                    Log.Debug($"{sender}Role creation failed: The role to be created is full");
                    return;
                }
                if (!StringHelper.NameVerify(request.Name))
                {
                    Log.Debug($"{sender}Character creation failed: Illegal character name");
                    sender.Send(new CharacterCreateResponse() { Error = NetError.IllegalCharacterName });
                    return;
                }
                var dbCharacter = SqlDb.FreeSql.Select<DbCharacter>()
                    .Where(p => p.Name == request.Name)
                    .First();
                if (dbCharacter != null)
                {
                    sender.Send(new CharacterCreateResponse() { Error = NetError.RepeatCharacterName });
                    Log.Debug($"{sender}Character creation failed: Character name already exists");
                    return;
                }

                var mapId = MapManager.Instance.InitMapId;
                var initPos = DataHelper.ParseVector3(MapManager.Instance.GetMapById(mapId).Define.InitPos);

                var newDbCharacter = new DbCharacter()
                {
                    Id = 0,
                    Name = request.Name,
                    UserId = sender.User.UserId,
                    UnitId = request.UnitId,
                    MapId = mapId,
                    Level = 0,
                    Exp = 0,
                    Gold = 0,
                    Hp = 884,
                    Mp = 516,
                    X = (int)initPos.X,
                    Y = (int)initPos.Y,
                    Z = (int)initPos.Z,
                    Knapsack = null,
                };
                long characterId = SqlDb.FreeSql.Insert(newDbCharacter).ExecuteIdentity();
                if (characterId == 0)
                {
                    sender.Send(new CharacterCreateResponse() { Error = NetError.UnknowError });
                    Log.Debug($"{sender}Character creation failed: Database error");
                    return;
                }

                newDbCharacter.Id = characterId;
                sender.Send(new CharacterCreateResponse()
                {
                    Character = newDbCharacter.ToNetCharacter(),
                    Error = NetError.Success
                });
                Log.Debug($"{sender}Role created successfully");
            
            });
        }

        public void OnHandle(NetChannel sender, CharacterListRequest request)
        {
            UpdateManager.Instance.AddTask(() =>
            {
                Log.Debug($"{sender}Role list query request");
                if (sender.User == null)
                {
                    Log.Debug($"{sender}Role list query failed: User not logged in");
                    return;
                }

                var characterList = SqlDb.FreeSql.Select<DbCharacter>()
                    .Where(t => t.UserId.Equals(sender.User.UserId))
                    .ToList();
                var res = new CharacterListResponse();
                foreach (var character in characterList)
                {
                    res.CharacterList.Add(character.ToNetCharacter());
                }

                sender.Send(res, null);
                Log.Debug($"{sender}Role list query successful");
            });
        }

        public void OnHandle(NetChannel sender, CharacterDeleteRequest request)
        {
            UpdateManager.Instance.AddTask(() =>
            {
                Log.Debug($"{sender}Role deletion request");
                if (sender.User == null)
                {
                    Log.Debug($"{sender}Role deletion failed: User not logged in");
                    return;
                }

                var deleteCount = SqlDb.FreeSql.Delete<DbCharacter>()
                    .Where(t => t.UserId.Equals(sender.User.UserId))
                    .Where(t => t.Id == request.CharacterId)
                    .ExecuteAffrows();
                sender.Send(new CharacterDeleteResponse() { Error = NetError.Success });
                Log.Debug($"{sender}Role deleted successfully");
            });
        }

    }
}
