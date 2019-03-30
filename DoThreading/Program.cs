using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

// http://www.java2s.com/Tutorial/CSharp/0420__Thread/QueuingataskforexecutionbyThreadPoolthreadswiththeRegisterWaitForSingleObjectmethod.htm
// https://docs.microsoft.com/en-us/dotnet/api/system.threading.threadpool.queueuserworkitem?view=netframework-4.7.2#System_Threading_ThreadPool_QueueUserWorkItem_System_Threading_WaitCallback_System_Object_
// https://docs.microsoft.com/en-us/dotnet/api/system.threading.waithandle.waitall?view=netframework-4.7.2

namespace DoThreading
{
    class Program
    {
        private static int NUM_THREADS = 100;
        private static int NUM_FILES = 1000;

        private static string remotePath = @"\\10.0.0.3\joe\temp_test";

        public static void Main()
        {
            CreateLotsOfFiles(remotePath);
            Thread.Sleep(1000);

            ThreadPool.SetMaxThreads(NUM_THREADS, 10);
            ThreadPool.SetMinThreads(NUM_THREADS, 10);

            string[] fileEntries = Directory.GetFiles(remotePath);

            ThreadState[] stateInfos = new ThreadState[fileEntries.Length];

            Console.WriteLine("Queueing deletes for " + fileEntries.Length + " files.");
            for (int i = 0; i < fileEntries.Length; i++)
            {
                stateInfos[i] = new ThreadState(fileEntries[i]);
                ThreadPool.QueueUserWorkItem(new WaitCallback(DeleteFile), stateInfos[i]);
            }

            // Since ThreadPool threads are background threads, 
            // wait for the work items to signal before exiting.
            while(stateInfos.Count(s => !s.isThreadCompleted) > 0)
            {
                //Console.WriteLine("Count: " + stateInfos.Count(s => !s.isThreadCompleted));
                Thread.Sleep(2500);
            }

            Console.WriteLine("Completed all deletes.");
        }

        static void DeleteFile(object state)
        {
            ThreadState stateInfo = (ThreadState)state;

            try
            {
                File.Delete(stateInfo.fileName);
            }
            catch(Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
            }

            stateInfo.isThreadCompleted = true;
        }

        // https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.task.whenall?view=netframework-4.7.2
        static void CreateLotsOfFiles(string folderPath)
        {
            var tasks = new List<Task>();

            for (int i=0; i<NUM_FILES; i++)
            {
                string fileName = folderPath + "\\" + i + ".txt";
                tasks.Add(Task.Run(() => {
                    try
                    {
                        using (FileStream fs = File.Create(fileName)) { 
                            using (StreamWriter writer = new StreamWriter(fs))
                            {
                                writer.WriteLine("Example 1 written");
                            }
                        }
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine("  Exception: " + ex.Message + ", creating: " + fileName);
                    }
                }));
            }

            Task t = Task.WhenAll(tasks.ToArray());

            try
            {
                Console.WriteLine("Waiting for all files to be written...");
                t.Wait();
            }
            catch { }

            if (t.Status == TaskStatus.RanToCompletion)
                Console.WriteLine("All files succeeded.");
            else if (t.Status == TaskStatus.Faulted)
                Console.WriteLine("File attempts failed");
        }
    }
    class ThreadState
    {
        public string fileName;
        public Boolean isThreadCompleted = false;

        public ThreadState(string fileName)
        {
            this.fileName = fileName;
        }
    }
}
