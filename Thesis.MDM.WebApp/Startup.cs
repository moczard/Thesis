using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(Thesis.MDM.WebApp.Startup))]
namespace Thesis.MDM.WebApp
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
