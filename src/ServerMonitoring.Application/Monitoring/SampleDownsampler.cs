using ServerMonitoring.Domain.Exceptions;

namespace ServerMonitoring.Application.Monitoring;

public static class SampleDownsampler
{
    public static IReadOnlyList<T> Downsample<T>(IReadOnlyList<T> samples, int maxPoints)
    {
        if (maxPoints <= 0)
            throw new ValidationException("حداکثر نقاط نمونه باید بزرگ‌تر از صفر باشد.");

        if (samples.Count <= maxPoints)
            return samples;

        var result = new T[maxPoints];
        var lastIndex = samples.Count - 1;
        for (var index = 0; index < maxPoints; index++)
        {
            var sourceIndex = index == maxPoints - 1
                ? lastIndex
                : (int)Math.Round(index * (lastIndex / (double)(maxPoints - 1)), MidpointRounding.AwayFromZero);
            result[index] = samples[sourceIndex];
        }

        return result;
    }
}
