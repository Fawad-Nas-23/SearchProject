using System;
using System.IO;
using Microsoft.Data.Sqlite;
using NLog;
namespace Indexer;
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                new App().Run();
            }
            finally
            {
                LogManager.Shutdown();
            }


        }   
        
  
    }