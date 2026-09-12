using System;
using System.Runtime.Serialization;

namespace Domain
{
    /// <summary>
    /// Represents an error that occurs when a requested aggregate cannot be found.
    /// </summary>
    [Serializable]
    public class AggregateNotFoundException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AggregateNotFoundException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public AggregateNotFoundException(string message) : base(message)
        {
        }
    }
}
