using MathNet.Numerics.Distributions;


namespace Pricer.Numerics;

public enum OptionType
{
    Call,
    Put
}

public class BlackScholes
{
    private OptionType option;
    private double r; // Risk-free interest rate
    private double T; // Time to maturity
    private double sigma; // Volatility
    private double K; // Strike price
    private double S; // Underlying asset price
    private double q; // Dividend yield

    public BlackScholes(
        OptionType optionType,
        double riskFreeRate,
        double timeToMaturity,
        double volatility,
        double strike,
        double underlyingPrice,
        double dividendYield = 0.0)
    {
        option = optionType;
        r = riskFreeRate;
        T = timeToMaturity;
        sigma = volatility;
        K = strike;
        S = underlyingPrice;
        q = dividendYield;
    }

    // Cumulative normal distribution
    private static double N(double x)
        => Normal.CDF(0.0, 1.0, x);

    // Normal probability density function
    private static double n(double x)
        // => throw new NotImplementedException("Normal PDF not implemented;");
        => Normal.PDF(0.0, 1.0, x);



    // Black–Scholes price
    public double Price()
    {
        if (T <= 0 || sigma <= 0)
        {
            double intrinsic = option == OptionType.Call
                ? Math.Max(S - K, 0)
                : Math.Max(K - S, 0);

            return intrinsic;
        }

        double sqrtT = Math.Sqrt(T);

        double d1 = (Math.Log(S / K)
                    + (r - q + 0.5 * sigma * sigma) * T)
                    / (sigma * sqrtT);

        double d2 = d1 - sigma * sqrtT;

        double discountedSpot = S * Math.Exp(-q * T);
        double discountedStrike = K * Math.Exp(-r * T);

        if (option == OptionType.Call)
        {
            return discountedSpot * N(d1)
                 - discountedStrike * N(d2);
        }
        else
        {
            return discountedStrike * N(-d2)
                 - discountedSpot * N(-d1);
        }
    }

    public double Vega()
    {
        if (T <= 0) return 0.0;

        double sqrtT = Math.Sqrt(T);

        double d1 = (Math.Log(S / K)
                    + (r - q + 0.5 * sigma * sigma) * T)
                    / (sigma * sqrtT);

        return S * Math.Exp(-q * T) * sqrtT * n(d1);
    }

}

// Test price compared with party at the mooonlight option calculator
// Test put call parity for consistency


// ========================================================
// Implied Volatility
// ========================================================

public static class ImpliedVolatilityCalculator
{
    public static double Compute(
        OptionType optionType,
        double marketPrice,
        double interestRate,
        double timeToMaturity,
        double strike,
        double underlyingPrice,
        double initialGuess = 0.2,
        double tolerance = 1e-8,
        int maxIterations = 100)
    {
        // throw new NotImplementedException("Implied volatility calculation not implemented");
        double sigma = initialGuess;

        for (int i = 0; i < maxIterations; i++)
        {
            var bs = new BlackScholes(
                optionType,
                interestRate,
                timeToMaturity,
                sigma,
                strike,
                underlyingPrice);

            double price = bs.Price();
            double vega = bs.Vega();

            double diff = price - marketPrice;

            if (Math.Abs(diff) < tolerance)
                return sigma;

            if (Math.Abs(vega) < 1e-10)
                break;

            sigma -= diff / vega;

            // Keep volatility positive
            sigma = Math.Max(sigma, 1e-8);
        }

        throw new Exception("Implied volatility did not converge.");
    }

    // ========================================================
    // Bachelier ATM Implied Volatility (Closed Form)
    // ========================================================
    // ATM formula:
    // σ = Price * sqrt(2π) / (S * sqrt(T))
    //
    // Assumes forward = strike
    // ========================================================
    public static double BachelierImpliedVolATM(
        double optionPrice,
        double underlyingPrice,
        double timeToMaturity,
        double interestRate)
    {
        // throw new NotImplementedException("Bachelier implied volatility calculation not implemented");
        if (timeToMaturity <= 0)
            throw new ArgumentException("Time to maturity must be positive.");

        double discountFactor = Math.Exp(-interestRate * timeToMaturity);

        // Remove discounting
        double undiscountedPrice = optionPrice / discountFactor;

        return undiscountedPrice
               * Math.Sqrt(2.0 * Math.PI)
               / (underlyingPrice * Math.Sqrt(timeToMaturity));
    }
}