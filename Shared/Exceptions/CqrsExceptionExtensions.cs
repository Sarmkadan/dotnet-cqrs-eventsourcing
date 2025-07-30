using System;

namespace DotNetCqrsEventSourcing.Shared.Exceptions
{
    /// <summary>
    /// Provides extension methods for <see cref="CqrsException"/> and related exception types
    /// to support fluent error handling and exception transformation scenarios.
    /// </summary>
    public static class CqrsExceptionExtensions
    {
        /// <summary>
        /// Creates a new <see cref="CqrsException"/> with the same error details but a new correlation ID.
        /// </summary>
        /// <param name="exception">The original exception.</param>
        /// <param name="newCorrelationId">The new correlation ID to use.</param>
        /// <returns>A new <see cref="CqrsException"/> instance.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/>.</exception>
        public static CqrsException WithCorrelationId(this CqrsException exception, string newCorrelationId)
        {
            ArgumentNullException.ThrowIfNull(exception);

            return new CqrsException(
                exception.Message,
                exception.ErrorCode,
                innerException: exception.InnerException,
                correlationId: newCorrelationId);
        }

        /// <summary>
        /// Creates a new <see cref="CqrsException"/> with the same details but updated occurred timestamp.
        /// </summary>
        /// <param name="exception">The original exception.</param>
        /// <param name="occurredAt">The new occurred timestamp.</param>
        /// <returns>A new <see cref="CqrsException"/> instance.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/>.</exception>
        public static CqrsException WithOccurredAt(this CqrsException exception, DateTime occurredAt)
        {
            ArgumentNullException.ThrowIfNull(exception);

            return new CqrsException(
                exception.Message,
                exception.ErrorCode,
                exception.InnerException,
                exception.CorrelationId)
            {
                OccurredAt = occurredAt
            };
        }

        /// <summary>
        /// Creates a new <see cref="CqrsException"/> that wraps this exception with a standardized error code.
        /// </summary>
        /// <param name="exception">The original exception.</param>
        /// <param name="errorCode">The standardized error code to use.</param>
        /// <returns>A new <see cref="CqrsException"/> instance.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="errorCode"/> is <see langword="null"/> or empty.</exception>
        public static CqrsException WithErrorCode(this CqrsException exception, string errorCode)
        {
            ArgumentNullException.ThrowIfNull(exception);
            ArgumentException.ThrowIfNullOrEmpty(errorCode);

            return new CqrsException(
                exception.Message,
                errorCode,
                innerException: exception.InnerException,
                correlationId: exception.CorrelationId);
        }

        /// <summary>
        /// Creates a new <see cref="AggregateNotFoundException"/> that preserves the original exception details.
        /// </summary>
        /// <param name="exception">The original exception.</param>
        /// <param name="aggregateType">The type of aggregate that was not found.</param>
        /// <param name="aggregateId">The ID of the missing aggregate.</param>
        /// <returns>A new <see cref="AggregateNotFoundException"/> instance.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="exception"/> or <paramref name="aggregateType"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException"><paramref name="aggregateId"/> is <see langword="null"/>, empty, or consists only of whitespace.</exception>
        public static AggregateNotFoundException ToAggregateNotFoundException(
            this CqrsException exception,
            Type aggregateType,
            string aggregateId)
        {
            ArgumentNullException.ThrowIfNull(exception);
            ArgumentNullException.ThrowIfNull(aggregateType);
            ArgumentException.ThrowIfNullOrWhiteSpace(aggregateId);

            var result = new AggregateNotFoundException(aggregateId, aggregateType.Name);

            // Preserve the correlation ID and occurred timestamp from the original exception
            if (exception.CorrelationId is not null)
            {
                result.CorrelationId = exception.CorrelationId;
            }

            if (exception.OccurredAt != default)
            {
                result.OccurredAt = exception.OccurredAt;
            }

            return result;
        }
    }
}
