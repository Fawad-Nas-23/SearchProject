using System;
using System.Threading.Tasks;

namespace ConsoleSearch;
class Program
{
    static async Task Main(string[] args)
    {
        var app = new App();
        await app.Run();
    }
}

