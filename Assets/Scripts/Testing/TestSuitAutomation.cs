using System.Collections.Generic;
using Engine;
using Model;
using static Model.Cubie;
using static Manager;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using System;

namespace Testing
{
    public static class TestSuitAutomation
    {
        private static SemaphoreSlim _semaphore = new (6);
        private static List<Task> _tasks = new();

        public static async Task RunRandomTestsAsync(int frequency)
        {

            for (int i = 0; i < frequency; i++)
            {
                int testIndex = i; // capture loop variable

                await _semaphore.WaitAsync();

                var task = Task.Run(async () =>
                {
                    try
                    {
                        Cubie cube = new(Identity);
                        cube.Shuffle();

                        Solver solver = new(false, testIndex);
                        await solver.SolveAsync(cube, 0);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Test {testIndex} failed: {ex}");
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                });

                lock (_tasks)
                {
                    _tasks.Add(task);
                }
            }

            Task[] tasksCopy;
            lock (_tasks)
            {
                tasksCopy = _tasks.ToArray();
            }

            await Task.WhenAll(tasksCopy);
        }
    }
}