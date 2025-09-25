using MMORPG.Common.Proto.Base;
using MMORPG.Common.Proto.User;
using MMORPG.Common.Tool;
using MMORPG.Model;
using QFramework;
using MMORPG.System;
using Serilog;
using UnityEngine.SceneManagement;

namespace MMORPG.Command
{
    public class LoginCommand : AbstractCommand
    {
        private string _username;
        private string _password;

        public LoginCommand(string username, string password)
        {
            _username = username;
            _password = password;
        }

        protected override async void OnExecute()
        {
            var box = this.GetSystem<IBoxSystem>();
            if (_username.Length < 4 || _username.Length > 12)
            {
                box.ShowNotification("Username length must be between 4 and 12 characters!");
                return;
            }

            if (_password.Length < 8 || _password.Length > 16)
            {
                box.ShowNotification("The password must be between 8 and 16 characters long!");
                return;
            }

            box.ShowSpinner("Logging in......");
            var net = this.GetSystem<INetworkSystem>();
            net.SendToServer(new LoginRequest
            {
                Username = _username,
                Password = _password
            });
            var response = await net.ReceiveAsync<LoginResponse>();
            box.CloseSpinner();

            if (response.Error == NetError.Success)
            {
                Log.Information($"'{_username}'Login successful");
                var user = this.GetModel<IUserModel>();
                user.Username.Value = _username;
                SceneManager.LoadScene("CharacterSelectScene");
            }
            else
            {
                Log.Error($"'{_username}'Login failed:{response.Error.GetInfo().Description}");
                box.ShowNotification($"Login failed!\n reason:{response.Error.GetInfo().Description}");
            }
        }
    }
}
