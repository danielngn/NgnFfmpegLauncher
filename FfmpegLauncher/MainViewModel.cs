﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using FfmpegLauncher.Models;
using FfmpegLauncher.Properties;

namespace FfmpegLauncher
{
    public class MainViewModel : NotifiableBase
    {
        private readonly object _tasksLock = new object();
        private Dispatcher _uiDispatcher;

        public MainViewModel()
        {
            PersistAllSettings(PersistDirection.Load);
            _uiDispatcher = Dispatcher.CurrentDispatcher;
            BindingOperations.EnableCollectionSynchronization(Tasks, _tasksLock);
        }

        public ObservableCollection<TaskBase> Tasks { get; } = new ObservableCollection<TaskBase>();

        private ICommand _AddConvertCommand;
        public ICommand AddConvertCommand
        {
            get
            {
                if (_AddConvertCommand == null)
                    _AddConvertCommand = new RelayCommand(x =>
                    {
                        var task = new ConvertTask();
                        SetTaskDefault(task);
                        Tasks.Add(task);
                    });
                return _AddConvertCommand;
            }
        }

        private ICommand _AddMergeCommand;
        public ICommand AddMergeCommand
        {
            get
            {
                if (_AddMergeCommand == null)
                    _AddMergeCommand = new RelayCommand(x =>
                    {
                        var task = new MergeTask();
                        SetTaskDefault(task);
                        Tasks.Add(task);
                    });
                return _AddMergeCommand;
            }
        }

        private ICommand _RemoveTaskCommand;
        public ICommand RemoveTaskCommand
        {
            get
            {
                if (_RemoveTaskCommand == null)
                    _RemoveTaskCommand = new RelayCommand(x =>
                    {
                        if (SelectedTask != null)
                        {
                            SelectedTask.AddLog += Task_AddLog;
                            Tasks.Remove(SelectedTask);
                            SelectedTask = null;
                        }
                    }, x =>
                    {
                        return SelectedTask != null;
                    });
                return _RemoveTaskCommand;
            }
        }

        private ICommand _RunAllCommand;
        public ICommand RunAllCommand
        {
            get
            {
                if (_RunAllCommand == null)
                    _RunAllCommand = new RelayCommand(x => RunAll(), x =>
                     {
                         return Tasks.Any(t => t.RunCommand.CanExecute(null));
                     });
                return _RunAllCommand;
            }
        }

        private async void RunAll()
        {
            foreach (var task in Tasks)
            {
                await task.RunTask();
            }
        }

        private TaskBase _selectedTask;
        public TaskBase SelectedTask
        {
            get { return _selectedTask; }
            set
            {
                _selectedTask = value;
                NotifyPropertyChanged(nameof(SelectedTask));
            }
        }

        private void SetTaskDefault(TaskBase task)
        {
            task.UiDispatcher = _uiDispatcher;
            task.FfmpegFolder = FfmpegFolder;
            task.IsEditable = true;
            task.UseHardwareDecode = true;
            task.UseHardwareEncode = true;
            task.BitRate = DefaultBitRate;
            if (DefaultMaxBitRate > DefaultBitRate)
            {
                task.MaxBitRate = DefaultMaxBitRate;
            }
            else
            {
                task.MaxBitRate = DefaultBitRate;
            }
            task.AddLog += Task_AddLog;
        }

        private void Task_AddLog(object sender, LogEventArgs e)
        {
            _uiDispatcher.BeginInvoke(new Action(() =>
                {
                    Logs.Insert(0, new LogItem() { Category = e.Category, Message = e.Message });
                }));
        }

        public int DefaultBitRate { get; set; }

        public int DefaultMaxBitRate { get; set; }

        public string FfmpegFolder { get; set; }

        public ObservableCollection<LogItem> Logs { get; } = new ObservableCollection<LogItem>();


        #region Persistence
        private PropertyInfo[] _persistedSettings;
        public PropertyInfo[] PersistedSettings
        {
            get
            {
                if (_persistedSettings == null)
                    _persistedSettings = Settings.Default.GetType().GetProperties().Where(x => x.CustomAttributes.Any(y => y.AttributeType == typeof(System.Configuration.UserScopedSettingAttribute))).ToArray();

                return _persistedSettings;
            }
        }

        private PropertyInfo[] _myProps;
        public PropertyInfo[] MyProps
        {
            get
            {
                if (_myProps == null)
                    _myProps = typeof(MainViewModel).GetProperties();

                return _myProps;
            }
        }

        private void PersistAllSettings(PersistDirection direction)
        {
            if (direction == PersistDirection.Load && Settings.Default.UpgradeSetting)
            {
                Settings.Default.Upgrade();
                Settings.Default.UpgradeSetting = false;
                Settings.Default.Save();
            }
            foreach (var setting in PersistedSettings)
            {
                var vmProp = MyProps.FirstOrDefault(x => x.Name.Equals(setting.Name, StringComparison.InvariantCultureIgnoreCase));
                if (vmProp != null)
                {
                    switch (direction)
                    {
                        case PersistDirection.Load:
                            vmProp.SetValue(this, setting.GetValue(Settings.Default));
                            break;
                        case PersistDirection.Save:
                            setting.SetValue(Settings.Default, vmProp.GetValue(this));
                            break;
                    }
                }
            }
            if (direction == PersistDirection.Save)
                Settings.Default.Save();
        }

        public void SaveSetting()
        {
            PersistAllSettings(PersistDirection.Save);
        }

        public enum PersistDirection
        {
            Load,
            Save
        }
        #endregion
    }
}
