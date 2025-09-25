using GameServer.EntitySystem;
using GameServer.MapSystem;
using GameServer.System;
using GameServer.Tool;
using GameServer.UserSystem;
using MMORPG.Common.Proto.Fight;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Manager
{
    /// <summary>
    /// Responsible for updating all components
    /// </summary>
    public class UpdateManager : Singleton<UpdateManager>
    {
        public readonly int Fps = 10;
        private Queue<Action> _taskQueue = new();
        private Queue<Action> _backupTaskQueue = new();

        private UpdateManager()
        {
        }

        public void Start()
        {
            DataManager.Instance.Start();
            Log.Information("[Server] DataManager Initialization completed");

            EntityManager.Instance.Start();
            Log.Information("[Server] EntityManager Initialization completed");

            MapManager.Instance.Start();
            Log.Information("[Server] MapManager Initialization completed");

            UserManager.Instance.Start();
            Log.Information("[Server] UserManager Initialization completed");

            Scheduler.Instance.Register(1000 / Fps, Update);
        }

        public void Update()
        {
            Time.Tick();

            lock (_taskQueue)
            {
                (_backupTaskQueue, _taskQueue) = (_taskQueue, _backupTaskQueue);
            }

            foreach (var task in _backupTaskQueue)
            {
                try
                {
                    task();
                }
                catch (Exception e)
                {
                    Log.Error(e, "[UpdateManager] task() An error occurs when");
                }
            }
            _backupTaskQueue.Clear();

            try
            {
                DataManager.Instance.Update();
            }
            catch (Exception e)
            {
                Log.Error(e, "[UpdateManager] DataManager.Instance.Update() An error occurs when");
            }

            try
            {
                EntityManager.Instance.Update();
            }
            catch (Exception e)
            {
                Log.Error(e, "[UpdateManager] EntityManager.Instance.Update() An error occurs when");
            }

            try
            {
                MapManager.Instance.Update();
            }
            catch (Exception e)
            {
                Log.Error(e, "[UpdateManager] MapManager.Instance.Update() An error occurs when");
            }

            try
            {
                UserManager.Instance.Update();
            }
            catch (Exception e)
            {
                Log.Error(e, "[UpdateManager] UserManager.Instance.Update() An error occurs when");
            }
        }

        /// <summary>
        /// Thread safety
        /// </summary>
        /// <param name="task"></param>
        public void AddTask(Action task)
        {
            lock (_taskQueue)
            {
                _taskQueue.Enqueue(task);
            }
        }
    }
}
