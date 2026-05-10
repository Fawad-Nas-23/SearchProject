using System;

namespace Indexer;

public class Config
{
    // the folder to be indexed - all .txt files in that folder (and subfolders)
    // will be indexed
    public static string FOLDER =
        Environment.GetEnvironmentVariable("INDEXER_FOLDER")
        ?? @"C:\6.Semester\ArkitekturPrincipper\seData\seData copy\small\compaq";    //public static string FOLDER = @"C:\Users\hhw31\OneDrive\Skrivebord\ITSem6\ArkitekturPrincipper\seData\large\arnold-j";

    public static string RABBITMQ_HOST =
    Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost";

    public static int RABBITMQ_PORT =
        int.TryParse(Environment.GetEnvironmentVariable("RABBITMQ_PORT"), out var p) ? p : 5672;

    public static string RABBITMQ_USER =
        Environment.GetEnvironmentVariable("RABBITMQ_USER") ?? "guest";

    public static string RABBITMQ_PASSWORD =
        Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") ?? "guest";

    public static string RABBITMQ_QUEUE =
        Environment.GetEnvironmentVariable("RABBITMQ_QUEUE") ?? "indexer.events";

    public static string POSTGRES_CONNECTION =
       Environment.GetEnvironmentVariable("POSTGRES_CONNECTION")
       ?? "Host=127.0.0.1;Port=5432;Username=postgres;Password=1234;Database=SearchDB";

    public static string DATABASE_TYPE =
        Environment.GetEnvironmentVariable("INDEXER_DATABASE") ?? "";

}

