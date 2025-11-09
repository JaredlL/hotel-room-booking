using Microsoft.EntityFrameworkCore;
using Npgsql;
using Polly;
using Polly.Retry;

namespace HotelRoomBooking.Data;

public static class ResilienceStrategies
{
    /// <summary>
    /// Resilience strategy to handle rare Postgres Unique constraints that may be caused by racing requests.
    /// </summary>
    public static ResiliencePipeline<IResult> UniqueConstraintRaceConditionStrategy { get; } =
        new ResiliencePipelineBuilder<IResult>()
            .AddRetry(new RetryStrategyOptions<IResult>
            {
                // Handle Postgres Unique constraint violation
                ShouldHandle = new PredicateBuilder<IResult>()
                    .Handle<DbUpdateException>(ex =>
                        ex.InnerException is PostgresException { SqlState: "23505" }),

                // It is assumed that this strategy is used to retry in the case of race conditions.
                // These race conditions are expected to be rare. Therefore, a small number of retries are attempted
                // to maintain a reasonable response time.
                MaxRetryAttempts = 2,
                Delay = TimeSpan.FromMilliseconds(50),
                BackoffType = DelayBackoffType.Exponential,
            })
            .Build();
}