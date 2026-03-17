namespace Pricer.Numerics;

public static class BrownianMotionGenerator
{
    /// <summary>
    /// Computes the per-step standard deviation: σ · (Δt)^(α/2)
    /// </summary>
    public static double ComputeScale(int numSteps, double sigma, double alpha)
    {
        double dt = 1.0 / numSteps;
        return sigma * Math.Pow(dt, alpha / 2.0);
    }

    /// <summary>
    /// Generates a single scaled random walk path as (times, values) arrays.
    /// </summary>
    public static (DateTime[] Times, double[] Values) GeneratePath(
        int numSteps, double dt, double scale, DateTime start, int seed)
    {
        var rng = new Random(seed);
        var times = new DateTime[numSteps + 1];
        var values = new double[numSteps + 1];

        times[0] = start;
        values[0] = 0;

        for (int i = 1; i <= numSteps; i++)
        {
            double increment = (rng.NextDouble() < 0.5 ? -1.0 : 1.0) * scale;
            values[i] = values[i - 1] + increment;
            times[i] = start.AddSeconds(i * dt * 3600);
        }

        return (times, values);
    }

    /// <summary>
    /// Generates two correlated Brownian paths using Cholesky decomposition:
    ///   W1 = Z1
    ///   W2 = ρ·Z1 + √(1−ρ²)·Z2
    /// where Z1, Z2 are independent standard Brownian motions.
    /// </summary>
    public static (double[] Path1, double[] Path2) GenerateCorrelatedPaths(
        int numSteps, double sigma, double rho, int seed = 42)
    {
        double dt = 1.0 / numSteps;
        double sqrtDt = Math.Sqrt(dt);
        double sqrtOneMinusRhoSq = Math.Sqrt(Math.Max(0, 1 - rho * rho));
        var rng = new Random(seed);

        var path1 = new double[numSteps + 1];
        var path2 = new double[numSteps + 1];

        // Start both paths at 0
        path1[0] = 0;
        path2[0] = 0;

        for (int i = 1; i <= numSteps; i++)
        {
            // Box-Muller transform: two independent standard normals
            double u1 = 1.0 - rng.NextDouble();
            double u2 = 1.0 - rng.NextDouble();
            double z1 = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
            double z2 = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);

            // Cholesky: correlate the two increments
            double dW1 = sigma * sqrtDt * z1;
            double dW2 = sigma * sqrtDt * (rho * z1 + sqrtOneMinusRhoSq * z2);

            path1[i] = path1[i - 1] + dW1;
            path2[i] = path2[i - 1] + dW2;
        }

        return (path1, path2);
    }
}