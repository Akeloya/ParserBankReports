using Caliburn.Micro;

using System.Dynamic;
using System.Windows;

namespace ParserApp
{
    public class ShellBootstrapper<TModel> : Bootstrapper
    {

        public ShellBootstrapper()
        {

        }

        protected override void OnStartup(object sender, StartupEventArgs e)
        {
            base.OnStartup(sender, e);
            StartApplication();
        }

        public void StartApplication()
        {
            var inst = GetInstance(typeof(TModel), "");
            var manager = IoC.Get<IWindowManager>();
            dynamic settings = new ExpandoObject();
            settings.Height = 500;
            settings.Width = 800;
            settings.SizeToContent = SizeToContent.Manual;
            manager.ShowWindowAsync(inst, settings: settings);
        }
    }
}
