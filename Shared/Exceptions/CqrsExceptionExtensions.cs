using System;
using System.Collections.Generic;

namespace DotNetCqrsEventSourcing.Shared.Exceptions
{
    public static class CqrsExceptionExtensions
    {
        /// <summary>
        /// Creates a new CqrsException with the same error details but a new correlation ID.
        /// </summary>
        /// <param name="exception">The original exception</param>
        /// <param name="newCorrelationId">The new correlation ID to use</param>
        /// <returns>A new CqrsException instance</returns>
        public static CqrsException WithCorrelationId(this CqrsException exception, string newCorrelationId)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            return new CqrsException(
                exception.Message,
                exception.ErrorCode,
                innerException: exception.InnerException,
                correlationId: newCorrelationId);
        }

        /// <summary>
        /// Creates a new CqrsException with the same details but updated occurred timestamp.
        /// </summary>
        /// <param name="exception">The original exception</param>
        /// <param name="occurredAt">The new occurred timestamp</param>
        /// <returns>A new CqrsException instance</returns>
        public static CqrsException WithOccurredAt(this CqrsException exception, DateTime occurredAt)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            var result = new CqrsException(
                exception.Message,
                exception.ErrorCode,
                innerException: exception.InnerException,
                correlationId: exception.CorrelationId);

            // Use reflection to set the read-only OccurredAt property
            result.GetType().GetProperty("OccurredAt")?.SetValue(result, occurredAt);

            return result;
        }

        /// <summary>
        /// Creates a new CqrsException that wraps this exception with a standardized error code.
        /// </summary>
        /// <param name="exception">The original exception</param>
        /// <param name="errorCode">The standardized error code to use</param>
        /// <returns>A new CqrsException instance</returns>
        public static CqrsException WithErrorCode(this CqrsException exception, string errorCode)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            return new CqrsException(
                exception.Message,
                errorCode,
                innerException: exception.InnerException,
                correlationId: exception.CorrelationId);
        }

        /// <summary>
        /// Creates a new AggregateNotFoundException that preserves the original exception details.
        /// </summary>
        /// <param name="exception">The original exception</param>
        /// <param name="aggregateType">The type of aggregate that was not found</param>
        /// <param name="aggregateId">The ID of the missing aggregate</param>
        /// <returns>A new AggregateNotFoundException instance</returns>
        public static AggregateNotFoundException ToAggregateNotFoundException(
            this CqrsException exception,
            Type aggregateType,
            string aggregateId)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            if (aggregateType == null)
            {
                throw new ArgumentNullException(nameof(aggregateType));
            }

            if (string.IsNullOrWhiteSpace(aggregateId))
            {
                throw new ArgumentException("Aggregate ID cannot be null or whitespace", nameof(aggregateId));
            }

            var aggregateTypeName = aggregateType.Name;
            var result = new AggregateNotFoundException(aggregateId, aggregateTypeName);

            // Preserve the correlation ID and occurred timestamp from the original exception
            if (exception.CorrelationId != null)
            {
                result.GetType().GetProperty("CorrelationId")?.SetValue(result, exception.CorrelationId);
            }

            if (exception.OccurredAt != default)
            {
                result.GetType().GetProperty("OccurredAt")?.SetValue(result, exception.OccurredAt);
            }

            return result;
        }
    }
}
