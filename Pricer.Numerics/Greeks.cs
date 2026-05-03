using MathNet.Numerics.Distributions;

namespace Pricer.Numerics;

/// <summary>
/// Holds Price + the four Greeks for a single strategy leg or combined strategy.
/// </summary>
public record GreeksResult(double Price, double Delta, double Gamma, double Theta, double Vega);

/// <summary>
/// Computes BS price and Greeks for a single call or put, then combines them into strategies.
/// </summary>
public static class GreeksCalculator
{
    private static double N(double x) => Normal.CDF(0, 1, x);
    private static double n(double x) => Normal.PDF(0, 1, x);

    /// <summary>Single call or put Greeks.</summary>
    public static GreeksResult Compute(OptionType type, double S, double K,
                                       double r, double T, double sigma)
    {
        if (T <= 0 || sigma <= 0)
        {
            double intrinsic = type == OptionType.Call
                ? Math.Max(S - K, 0) : Math.Max(K - S, 0);
            double deltaAtExpiry = type == OptionType.Call
                ? (S > K ? 1 : 0) : (S < K ? -1 : 0);
            return new GreeksResult(intrinsic, deltaAtExpiry, 0, 0, 0);
        }

        double sqrtT = Math.Sqrt(T);
        double d1 = (Math.Log(S / K) + (r + 0.5 * sigma * sigma) * T) / (sigma * sqrtT);
        double d2 = d1 - sigma * sqrtT;

        double price, delta;
        if (type == OptionType.Call)
        {
            price = S * N(d1) - K * Math.Exp(-r * T) * N(d2);
            delta = N(d1);
        }
        else
        {
            price = K * Math.Exp(-r * T) * N(-d2) - S * N(-d1);
            delta = N(d1) - 1;
        }

        double gamma = n(d1) / (S * sigma * sqrtT);
        double vega  = S * n(d1) * sqrtT / 100.0; // per 1% move in vol
        double theta = type == OptionType.Call
            ? (-S * n(d1) * sigma / (2 * sqrtT) - r * K * Math.Exp(-r * T) * N(d2))  / 365.0
            : (-S * n(d1) * sigma / (2 * sqrtT) + r * K * Math.Exp(-r * T) * N(-d2)) / 365.0;

        return new GreeksResult(price, delta, gamma, theta, vega);
    }

    // ---- Strategy combinators ----

    /// <summary>Call spread: long call at K1, short call at K2 (K2 > K1).</summary>
    public static GreeksResult CallSpread(double S, double K1, double K2,
                                          double r, double T, double sigma)
    {
        var longCall  = Compute(OptionType.Call, S, K1, r, T, sigma);
        var shortCall = Compute(OptionType.Call, S, K2, r, T, sigma);
        return Combine(longCall, shortCall, +1, -1);
    }

    /// <summary>Straddle: long call + long put at same strike.</summary>
    public static GreeksResult Straddle(double S, double K,
                                        double r, double T, double sigma)
    {
        var call = Compute(OptionType.Call, S, K, r, T, sigma);
        var put  = Compute(OptionType.Put,  S, K, r, T, sigma);
        return Combine(call, put, +1, +1);
    }

    /// <summary>Butterfly: long call K1, short 2 calls K2, long call K3.</summary>
    public static GreeksResult Butterfly(double S, double K1, double K2, double K3,
                                         double r, double T, double sigma)
    {
        var c1 = Compute(OptionType.Call, S, K1, r, T, sigma);
        var c2 = Compute(OptionType.Call, S, K2, r, T, sigma);
        var c3 = Compute(OptionType.Call, S, K3, r, T, sigma);
        return new GreeksResult(
            c1.Price  - 2 * c2.Price  + c3.Price,
            c1.Delta  - 2 * c2.Delta  + c3.Delta,
            c1.Gamma  - 2 * c2.Gamma  + c3.Gamma,
            c1.Theta  - 2 * c2.Theta  + c3.Theta,
            c1.Vega   - 2 * c2.Vega   + c3.Vega);
    }

    private static GreeksResult Combine(GreeksResult a, GreeksResult b, double wA, double wB) =>
        new(wA * a.Price + wB * b.Price,
            wA * a.Delta + wB * b.Delta,
            wA * a.Gamma + wB * b.Gamma,
            wA * a.Theta + wB * b.Theta,
            wA * a.Vega  + wB * b.Vega);
}
