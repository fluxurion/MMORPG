using MMORPG.Common.Tool;
using GameServer.Tool;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;
using System.Data;
using GameServer.Manager;

namespace GameServer.MapSystem
{

    /// <summary>
    /// map manager
    /// Responsible for managing all maps of the game
    /// </summary>
    public class MapManager : Singleton<MapManager>
    {
        public readonly int InitMapId = 1;

        private Dictionary<int, Map> _mapDict = new();

        MapManager() { }

        public void Start()
        {
            foreach (var mapDefine in DataManager.Instance.MapDict.Values)
            {
                Log.Information($"Load map：{mapDefine.Name}");
                NewMap(mapDefine);
            }
        }

        public void Update()
        {
            foreach (var map in _mapDict.Values)
            {
                map.Update();
            }
        }

        private Map NewMap(MapDefine mapDefine)
        {
            var map = new Map(mapDefine);
            _mapDict.Add(mapDefine.ID, map);
            map.Start();
            return map;
        }

        public Map? GetMapById(int mapId)
        {
            _mapDict.TryGetValue(mapId, out var map);
            return map;
        }

        public Map? GetMapByName(string mapName)
        {
            foreach (var map in _mapDict)
            {
                if (map.Value.Define.Name == mapName)
                {
                    return map.Value;
                }
            }
            return null;
        }
    }
}
