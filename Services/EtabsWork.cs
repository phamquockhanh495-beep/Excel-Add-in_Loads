using System;
using System.Threading.Tasks;
namespace QksExcelAddIn.Services
{
    internal static class EtabsWork
    {
        // Ribbon windows share one STA. Execute COM work on that apartment, never the thread pool.
        internal static Task<T> RunAsync<T>(Func<T> operation) => Task.FromResult(operation());
    }
}
