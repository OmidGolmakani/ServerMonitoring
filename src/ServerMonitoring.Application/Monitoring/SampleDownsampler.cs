using ServerMonitoring.Domain.Exceptions;

namespace ServerMonitoring.Application.Monitoring;

public static class SampleDownsampler
{
    public static IReadOnlyList<T> Downsample<T>(IReadOnlyList<T> source, int maxPoints)
    {
        if (maxPoints <= 0)
            throw new ValidationException("تعداد نقاط نمودار نامعتبر است.");

        if (source.Count <= maxPoints)
            return source;

        if (maxPoints == 1)
            return [source[^1]];

        var result = new List<T>(maxPoints);
        var step = (source.Count - 1) / (double)(maxPoints - 1);
        for (var index = 0; index < maxPoints; index++)
        {
            var sourceIndex = (int)Math.Round(index * step, MidpointRounding.AwayFromZero);
            result.Add(source[Math.Clamp(sourceIndex, 0, source.Count - 1)]);
        }

        return result;
    }
}
