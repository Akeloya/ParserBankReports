using System;
using System.Collections.Generic;
using Caliburn.Micro;
using Ninject;

using ParserApp.Services;

namespace ParserApp
{
    public class Bootstrapper : BootstrapperBase
    {
        private readonly IKernel _kernel;
        public Bootstrapper()
        {
            _kernel = new StandardKernel();
        }

        protected override void Configure()
        {
            base.Configure();
            _kernel.Rebind<IWindowManager>().To<WindowManager>().InSingletonScope();
            _kernel.Bind<MainWindowViewModel>().ToSelf().InSingletonScope();
            _kernel.Bind<IDialogService>().To<DialogService>().InSingletonScope();
        }

        protected override object GetInstance(Type service, string Key){
            if(service == null)
                throw new ArgumentNullException(nameof(service));
            return _kernel.Get(service);
        }

        protected override IEnumerable<object> GetAllInstances(Type service){

            return _kernel.GetAll(service);
        }

        protected override void BuildUp(object instance){
            _kernel.Inject(instance);
        }

        protected override void OnExit(object sender, EventArgs e)
        {
            _kernel.Dispose();
            base.OnExit(sender, e);
        }

    }
}
