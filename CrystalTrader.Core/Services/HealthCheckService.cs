﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace CrystalTrader.Core
{
    internal class HealthCheckService : IHealthCheckService
    {
        private readonly ILoggingService loggingService;
        private readonly INotificationService notificationService;
        private readonly ITasksService tasksService;

        private readonly ConcurrentDictionary<string, HealthCheck> healthChecks = new ConcurrentDictionary<string, HealthCheck>();
        private HealthCheckTimedTask healthCheckTimedTask;

        public HealthCheckService(ILoggingService loggingService, INotificationService notificationService, ITasksService tasksService)
        {
            this.loggingService = loggingService;
            this.notificationService = notificationService;
            this.tasksService = tasksService;
        }

        public void Start()
        {
            loggingService.Info($"Start Health Check service...");

            healthCheckTimedTask = tasksService.AddTask(
                name: nameof(HealthCheckTimedTask),
                task: new HealthCheckTimedTask(loggingService, notificationService, this, Application.Resolve<ICoreService>(), Application.Resolve<ITradingService>()),
                interval: Application.Resolve<ICoreService>().Config.HealthCheckInterval * 1000 / Application.Speed,
                startDelay: Constants.TaskDelays.HighDelay,
                startTask: false,
                runNow: false,
                skipIteration: 0);

            loggingService.Info("Health Check service started");
        }

        public void Stop()
        {
            loggingService.Info($"Stop Health Check service...");

            tasksService.RemoveTask(nameof(HealthCheckTimedTask), stopTask: true);

            loggingService.Info("Health Check service stopped");
        }

        public void UpdateHealthCheck(string name, string message = null, bool failed = false)
        {
            if (!healthChecks.TryGetValue(name, out HealthCheck existingHealthCheck))
            {
                healthChecks.TryAdd(name, new HealthCheck
                {
                    Name = name,
                    Message = message,
                    LastUpdated = DateTimeOffset.Now,
                    Failed = failed
                });
            }
            else
            {
                healthChecks[name].Message = message;
                healthChecks[name].LastUpdated = DateTimeOffset.Now;
                healthChecks[name].Failed = failed;
            }
        }

        public void RemoveHealthCheck(string name)
        {
            healthChecks.TryRemove(name, out HealthCheck healthCheck);
        }

        public IEnumerable<IHealthCheck> GetHealthChecks()
        {
            foreach (var kvp in healthChecks)
            {
                yield return kvp.Value;
            }
        }
    }
}
