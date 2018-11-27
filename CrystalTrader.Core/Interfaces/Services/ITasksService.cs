﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace CrystalTrader.Core
{
    public interface ITasksService
    {
        T AddTask<T>(string name, T task, double interval, double startDelay = 0, bool startTask = true, bool runNow = false, int skipIteration = 0) where T : ITimedTask;
        void RemoveTask(string name, bool stopTask = true);
        void StartAllTasks();
        void StopAllTasks();
        void RemoveAllTasks();
        ITimedTask GetTask(string name);
        T GetTask<T>(string name);
        IEnumerable<KeyValuePair<string, ITimedTask>> GetAllTasks();
        void SetUnhandledExceptionHandler(UnhandledExceptionEventHandler handler);
    }
}
