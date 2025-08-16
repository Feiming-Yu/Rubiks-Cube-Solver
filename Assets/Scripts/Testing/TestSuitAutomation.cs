using System.Collections.Generic;
using Engine;
using Model;
using static Model.Cubie;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using System;

namespace Testing
{
    public static class TestSuitAutomation
    {
        // A semaphore limiting concurrency to 6 simultaneous tasks
        private static readonly SemaphoreSlim Semaphore = new(6);

        // A thread-safe list to track all running tasks
        private static readonly List<Task> Tasks = new();

        /// <summary>
        /// Runs multiple randomized cube-solving tests asynchronously,
        /// limiting the number of concurrent operations.
        /// </summary>
        /// <param name="frequency">Number of test runs to execute.</param>
        public static async Task RunRandomTestsAsync(int frequency)
        {
            for (int i = 0; i < frequency; i++)
            {
                int testIndex = i; // Capture the loop index for use inside the task

                await Semaphore.WaitAsync(); // Acquire a semaphore slot before starting a new task

                // Launch a new task to solve a shuffled cube
                var task = Task.Run(async () =>
                {
                    try
                    {
                        // Create and shuffle a new cube
                        Cubie cube = new(Identity);
                        cube.Scramble();

                        Solver solver = new(false, testIndex);
                        await solver.SolveAsync(cube, 0);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Test {testIndex} failed: {ex}");
                    }
                    finally
                    {
                        // Always release the semaphore slot when the task finishes
                        Semaphore.Release();
                    }
                });

                // Add the task to the global list in a thread-safe way
                lock (Tasks)
                {
                    Tasks.Add(task);
                }
            }

            // Take a snapshot of all tasks to await them outside the lock
            Task[] tasksCopy;
            lock (Tasks)
            {
                tasksCopy = Tasks.ToArray();
            }

            // Wait for all tasks to complete
            await Task.WhenAll(tasksCopy);
        }
    }
}