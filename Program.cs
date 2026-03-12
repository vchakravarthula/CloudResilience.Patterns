using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

// --- HEADER SECTION ---
// This project demonstrates 'Layered Resilience' for Distributed Systems.
// Targeted for .NET 10, utilizing the Polly library to manage transient faults.
// Logic based on planet-scale infrastructure patterns used in Azure Synapse/OneLake.

Console.WriteLine("🛡️ .NET 10 Resilience Lab: Retry + Circuit Breaker\n");

// --- STEP 1: DEFINE THE RETRY POLICY (THE FIRST LINE OF DEFENSE) ---
// We handle small 'blips' here. If an API call fails due to a network hiccup, 
// we don't want to trip the circuit immediately. We retry with Exponential Backoff.
var retryPolicy = Policy
    .Handle<Exception>()
    .WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)), // 2s, 4s, 8s backoff
        onRetry: (exception, sleepDuration, attemptNumber, context) =>
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"   [RETRY {attemptNumber}] Succeeded? No. Waiting {sleepDuration.TotalSeconds}s to avoid 'Thundering Herd'...");
            Console.ResetColor();
        });

// --- STEP 2: DEFINE THE CIRCUIT BREAKER (THE SYSTEM PROTECTOR) ---
// If the Retry Policy fails 2 times in a row, it means the outage is NOT a blip.
// The Circuit Breaker 'Opens' to stop the application from hammering a dying service.
var circuitBreakerPolicy = Policy
    .Handle<Exception>()
    .CircuitBreakerAsync(
        exceptionsAllowedBeforeBreaking: 2,           // Threshold for a 'Major Outage'
        durationOfBreak: TimeSpan.FromSeconds(10),    // Protection duration (Cool-down)
        onBreak: (ex, timespan) =>
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($">>> CIRCUIT OPEN: Protection active for {timespan.TotalSeconds}s. API calls blocked.");
            Console.ResetColor();
        },
        onReset: () =>
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(">>> CIRCUIT CLOSED: Service recovered. Requests resuming.");
            Console.ResetColor();
        },
        onHalfOpen: () =>
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine(">>> CIRCUIT HALF-OPEN: Sending a probe request to test health...");
            Console.ResetColor();
        }
    );

// --- STEP 3: WRAP THE POLICIES ---
// We wrap them so that the Retry Policy sits OUTSIDE the Circuit Breaker.
// This means: "Try to retry 3 times. If those retries fail, tell the Circuit Breaker."
var resilientStrategy = Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);

// --- STEP 4: THE SIMULATION LOOP ---
for (int i = 1; i <= 10; i++)
{
    Console.WriteLine($"\n--- Request Cycle #{i} ---");
    try
    {
        // Execute the combined strategy
        await resilientStrategy.ExecuteAsync(async () =>
        {
            // TOGGLE THIS: Set to 'true' to simulate an outage, 'false' for success.
            bool isServiceDown = true;

            if (isServiceDown)
                throw new Exception("Azure Service Timeout (Simulated Outage)");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Success: Data retrieved successfully.");
            Console.ResetColor();
            await Task.CompletedTask;
        });
    }
    catch (BrokenCircuitException)
    {
        // This catch block executes when the Circuit Breaker is OPEN.
        // It prevents the 'Retry' logic from even attempting the call.
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("Blocked by Circuit Breaker: Failing fast to save resources.");
        Console.ResetColor();
    }
    catch (Exception ex)
    {
        // Final catch for when all retries have failed and the circuit is open.
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Final Error: {ex.Message}");
        Console.ResetColor();
    }

    await Task.Delay(500); // Small delay for readability in console
}