using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;

namespace HealthyFromHomeApp
{
    public partial class App : Application
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            AllocConsole();

            Task.Run(() =>
            {
                var server = new Server.Server();
                server.Start();
            });

            // dokter
            var doctorWindow = new Doctor.LoginWindow();
            doctorWindow.Show();

            //client 1
            var loginWindow1 = new Clients.LoginWindow();
            loginWindow1.Show();

            //client 2
            var loginWindow2 = new Clients.LoginWindow();
            loginWindow2.Show();
        }
    }
}
