using MMORPG.Common.Proto.Entity;
using GameServer.Tool;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using GameServer.MapSystem;
using GameServer.EntitySystem;
using GameServer.Manager;

namespace GameServer.MonsterSystem
{
    /// <summary>
    /// monster manager
    /// Responsible for managing all monsters in the map
    /// </summary>
    public class MonsterManager
    {
        private Dictionary<int, Monster> _monsterDict = new();
        private Map _map;

        public MonsterManager(Map map)
        {
            _map = map;
        }

        public void Start()
        {

        }

        public void Update()
        {
        }

        public Monster NewMonster(SpawnDefine spawnDefine, int unitId, Vector3 pos, Vector3 dire, string name, int level)
        {
            var monster = new Monster(spawnDefine, EntityManager.Instance.NewEntityId(), DataManager.Instance.UnitDict[unitId], _map, pos, dire, name, level);
            EntityManager.Instance.AddEntity(monster);

            _monsterDict.Add(monster.EntityId, monster);
            
            _map.EntityEnter(monster);

            monster.Start();
            return monster;
        }

    }
}
