using MMORPG.Common.Proto.Base;
using MMORPG.Common.Proto.User;
using MMORPG.Common.Tool;
using QFramework;
using MMORPG.System;
using Serilog;


namespace MMORPG.Command
{
    public class RegisterCommand : AbstractCommand
    {
        private string _username;
        private string _password;
        private string _password2;

        public RegisterCommand(string username, string password, string password2)
        {
            _username = username;
            _password = password;
            _password2 = password2;
        }

        protected override async void OnExecute()
        {
            var box = this.GetSystem<IBoxSystem>();
            if (_username.Length is < 4 or > 12)
            {
                box.ShowNotification("Username must be between 4 and 12 characters long!");
                return;
            }
            if (_password.Length is < 8 or > 16)
            {
                box.ShowNotification("The password must be between 8 and 16 characters long!");
                return;
            }
            if (_password != _password2)
            {
                box.ShowNotification("The two passwords you entered are different!");
                return;
            }

            box.ShowSpinner("Registering......");
            var net = this.GetSystem<INetworkSystem>();
            net.SendToServer(new RegisterRequest
            {
                Username = _username,
                Password = _password
            });
            var response = await net.ReceiveAsync<RegisterResponse>();
            box.CloseSpinner();

            if (response.Error == NetError.Success)
            {
                Log.Information($"'{_username}'Registration successful!");
                box.ShowNotification("Registration successful!");
            }
            else
            {
                Log.Information($"'{_username}'Registration failed:{response.Error.GetInfo().Description}");
                box.ShowNotification($"Registration failed!\n reason:{response.Error.GetInfo().Description}");
            }
        }
    }

}
