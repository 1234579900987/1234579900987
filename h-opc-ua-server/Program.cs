using System.Configuration;
using Topshelf;

namespace Hylasoft.Opc
{
    static class Program
    {
        public static readonly string ServiceName = ConfigurationManager.AppSettings["ServiceName"];
        public static readonly string ServiceDisplayName = ConfigurationManager.AppSettings["ServiceDisplayName"];
        public static readonly string ServiceDescription = ConfigurationManager.AppSettings["ServiceDescription"];

        static void Main()
        {
            Global.InitializeConfiguration();
            Global.InitializeDependencies();

            HostFactory.Run(config =>
            {
                config.Service<ServiceLauncher>(host =>
                {
                    host.ConstructUsing(name => new ServiceLauncher());
                    host.WhenStarted(launcher => launcher.Start());
                    host.WhenStopped(launcher => launcher.Stop());
                });
                config.RunAsLocalSystem();

                config.SetServiceName(Program.ServiceName);
                config.SetDisplayName(Program.ServiceDisplayName);
                config.SetDescription(Program.ServiceDescription);
            });
        }
    }
}
