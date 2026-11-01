using Caliburn.Micro;

using Microsoft.WindowsAPICodePack.Dialogs;

using ParserApp.Controls;
using ParserApp.Properties;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace ParserApp.Services
{
    public interface IDialogService
    {
        Task ShowAsync(object model);
        Task ShowErrorAsync(object model);
        Task ShowPupupAsync(string message);
        Task OpenFileDialog(string name = null, string filterDisplayName ="", string extFilter = "*.*", Func<string, Task> callAfterOk = null);
        Task SaveFileDialog(string name, string filter, Func<string, Task> callAfterOk);
        Task SelectFolderDialog(string name = null, string filterDisplayName ="", string extFilter = "*.*", Func<string, Task> callAfterOk = null);
        Task PrintDialog(Func<PrintDialog, Task> callAfterOk);
    }
    public class DialogService : IDialogService
    {
        private readonly IWindowManager _manager;

        public DialogService(IWindowManager manager) 
        {
            _manager = manager;
        }
        public async Task ShowAsync(object model)
        {
            await Execute.OnUIThreadAsync(() => _manager.ShowDialogAsync(model));
        }

        public Task ShowErrorAsync(object model)
        {
            var prm = new Dictionary<string, object>
            {
                { "Title", "Ошибка" }
            };
            return _manager.ShowDialogAsync(model, null, prm);
        }

        public Task ShowPupupAsync(string message)
        {
            var popupVm = new PopupViewModel
            {
                DisplayText = message
            };
            return _manager.ShowPopupAsync(popupVm);//TODO: нужно переделать на низ окна и чтобы по времени исчезало
        }

        public async Task OpenFileDialog(string name = null, string filterDisplayName = "(All files)", string extFilter = "*.*", Func<string, Task> callAfterOk = null)
        {
            string fileName = null;
            await Execute.OnUIThreadAsync(() =>
            {
                using (var dialog = new CommonOpenFileDialog(name ?? Resources.MenuItemOpenHtmlFile))
                {
                    dialog.Filters.Add(new CommonFileDialogFilter(filterDisplayName, extFilter));
                    if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                        fileName = dialog.FileName;
                }
                return Task.CompletedTask;
            });
            if (fileName != null && callAfterOk != null)
                await Execute.OnUIThreadAsync(() => callAfterOk(fileName));
        }
        public async Task SaveFileDialog(string name, string filter, Func<string, Task> callAfterOk)
        {
            string fileName = null;
            await Execute.OnUIThreadAsync(() =>
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = name,
                    Filter = filter,
                    AddExtension = true,
                    OverwritePrompt = true
                };
                if (dialog.ShowDialog() == true)
                    fileName = dialog.FileName;
                return Task.CompletedTask;
            });
            if (fileName != null)
                await callAfterOk(fileName);
        }
        public Task SelectFolderDialog(string name = null, string filterDisplayName ="(All files)", string extFilter = "*.*", Func<string,Task> callAfterOk = null)
        {
            return Task.Run(() =>
            {
                var ofd = new CommonOpenFileDialog(name ?? Resources.MenuItemOpenHtmlFile)
                {
                    IsFolderPicker = true
                };
                ofd.Filters.Add(new CommonFileDialogFilter(filterDisplayName, extFilter));

                CommonFileDialogResult result = default;

                Execute.OnUIThread(() =>
                {
                    result = ofd.ShowDialog();
                });

                if (result == CommonFileDialogResult.Ok && callAfterOk != null)
                    return callAfterOk(ofd.FileName);

                return Task.CompletedTask;
            });
             
        }

        public Task PrintDialog(Func<PrintDialog, Task> callAfterOk = null)
        {
            var result = false;
            PrintDialog printDialog = new PrintDialog();
            Execute.OnUIThread(() =>
            {
                result = printDialog.ShowDialog() ?? false;
            });
            if(result && callAfterOk != null)
                return callAfterOk(printDialog);
            return Task.CompletedTask;
        }
    }
}
