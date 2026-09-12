using System;

namespace Saga
{
    /// <summary>
    /// Coordinates long-running operations that span multiple messages or transactions.
    /// </summary>
    public class Saga
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Saga"/> class.
        /// </summary>
        public Saga()
        {
        }

        /// <summary>
        /// Requests that a message be delivered to a saga after a specified delay.
        /// </summary>
        /// <param name="sagaId">The identifier of the saga that should receive the message.</param>
        /// <param name="delay">The amount of time to wait before delivering the message.</param>
        /// <param name="message">The message to deliver when the delay has elapsed.</param>
        public void RequestTimeout(string sagaId, int delay, string message)
        {
            // TO DO: implement timeout logic
        }
    }
}
