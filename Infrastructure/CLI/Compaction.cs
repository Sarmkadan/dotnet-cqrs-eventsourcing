using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.CLI
{
    /// <summary>
    /// Provides command-line operations for compacting stored events.
    /// </summary>
    public class Compaction
    {
        /// <summary>
        /// Compacts the specified events after optionally requesting user confirmation.
        /// </summary>
        /// <param name="events">The events to compact.</param>
        /// <param name="confirm">
        /// <see langword="true"/> to proceed without prompting; otherwise, prompts the user for confirmation.
        /// </param>
        /// <returns>A task that represents the asynchronous compaction operation.</returns>
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
