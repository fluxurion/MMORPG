using MMORPG.Common.Network;
using MMORPG.Common.Proto.Base;
using GameServer.Db;
using GameServer.Network;
using GameServer.Tool;
using Serilog;
using MMORPG.Common.Proto.User;
using GameServer.UserSystem;
using GameServer.Manager;

namespace GameServer.NetService
{
    // There may be logic that still needs to be locked
    public class UserService : ServiceBase<UserService>
    {
        private static readonly object _loginLock = new();
        private static readonly object _registerLock = new();

        public void OnChannelClosed(NetChannel sender)
        {
            UpdateManager.Instance.AddTask(() =>
            {
                if (sender.User == null)
                    return;

                if (sender.User.Player != null)
                {
                    sender.User.Player.Valid = false;
                }

                UserManager.Instance.RemoveUser(sender.User.DbUser.Username);
            });
        }

        // TODO: Verify the legitimacy of username and password (length, etc.)
        public void OnHandle(NetChannel sender, LoginRequest request)
        {
            UpdateManager.Instance.AddTask(() =>
            {
                Log.Debug($"{sender}Login request: Username={request.Username}, Password={request.Password}");
                
                if (sender.User != null)
                {
                    sender.Send(new LoginResponse() { Error = NetError.UnknowError });
                    Log.Debug($"{sender}Login failed: User already logged in");
                    return;
                }

                if (UserManager.Instance.GetUserByName(request.Username) != null)
                {
                    sender.Send(new LoginResponse() { Error = NetError.LoginConflict });
                    Log.Debug($"{sender}Login failed: The account has been logged in elsewhere");
                    return;
                }

                var dbUser = SqlDb.FreeSql.Select<DbUser>()
                    .Where(p => p.Username == request.Username)
                    .Where(p => p.Password == request.Password)
                    .First();
                if (dbUser == null)
                {
                    sender.Send(new LoginResponse() { Error = NetError.IncorrectUsernameOrPassword });
                    Log.Debug($"{sender}Login failed: Account or password incorrect");
                    return;
                }

                sender.SetUser(UserManager.Instance.NewUser(sender, dbUser));
            

                sender.Send(new LoginResponse() { Error = NetError.Success });
                Log.Debug($"{sender}Login successful");
            });
        }

        public void OnHandle(NetChannel sender, RegisterRequest request)
        {
            UpdateManager.Instance.AddTask(() =>
            {
                Log.Debug($"{sender}Registration request: Username={request.Username}, Password={request.Password}");
                if (sender.User != null)
                {
                    sender.Send(new RegisterResponse() { Error = NetError.UnknowError });
                    Log.Debug($"{sender}Registration failed: User already logged in");
                    return;
                }

                if (!StringHelper.NameVerify(request.Username))
                {
                    sender.Send(new RegisterResponse() { Error = NetError.IllegalUsername });
                    Log.Debug($"{sender}Registration failed: invalid username");
                    return;
                }

                var dbUser = SqlDb.FreeSql.Select<DbUser>()
                    .Where(p => p.Username == request.Username)
                    .First();
                if (dbUser != null)
                {
                    sender.Send(new RegisterResponse() { Error = NetError.RepeatUsername });
                    Log.Debug($"{sender}Registration failed: Username has already been registered");
                    return;
                }

                var newDbUser = new DbUser(request.Username, request.Password, Authoritys.Player);
                var insertCount = SqlDb.FreeSql.Insert<DbUser>(newDbUser).ExecuteAffrows();
                if (insertCount <= 0)
                {
                    sender.Send(new RegisterResponse() { Error = NetError.UnknowError });
                    Log.Debug($"{sender}Registration failed: Database error");
                    return;
                }

                sender.Send(new RegisterResponse() { Error = NetError.Success });
                Log.Debug($"{sender}Registration successful");
                
            });
        }

        public void OnHandle(NetChannel sender, HeartBeatRequest request)
        {
            UpdateManager.Instance.AddTask(() =>
            {
                Log.Debug($"{sender}Sending a heartbeat request");
                //sender.Send(new HeartBeatResponse() { }, null);
            });
        }

        public void OnConnect(NetChannel sender)
        {
        }
    }
}
