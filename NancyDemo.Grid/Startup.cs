using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(NancyDemo.Grid.Startup))]
namespace NancyDemo.Grid
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
            app.UseNancy();
        }
    }
}
