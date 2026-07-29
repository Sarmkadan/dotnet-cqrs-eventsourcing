using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.CLI
{
    public class Compaction
    {
        public async Task CompactEvents(string[] events, bool confirm = false)
        {
            if (!confirm)
            {
                Console.WriteLine("This operation will delete events. Are you sure you want to proceed? (y/n)");
                var response = Console.ReadLine();
                if (response.ToLower() != "y")
                {
                    return;
                }
            }
            // rest of your code here...
        }
    }
}