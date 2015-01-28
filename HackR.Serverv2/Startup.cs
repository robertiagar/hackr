using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(HackR.Serverv2.Startup))]
namespace HackR.Serverv2
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
            app.MapSignalR();
        }
    }
}
