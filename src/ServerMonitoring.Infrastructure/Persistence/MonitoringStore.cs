using Microsoft.Extensions.Options;
using ServerMonitoring.Application.Options;
using ServerMonitoring.Domain.Entities;

namespace ServerMonitoring.Infrastructure.Persistence;

public sealed class MonitoringStore(IOptions<MonitoringOptions> options)
{
    private readonly MonitoringOptions _options = options.Value;
    private readonly Queue<RequestTrace> _requests = new();
    private readonly Queue<RequestTrace> _pendingRequests = new();
    private readonly Queue<SystemSample> _samples = new();
    private readonly Queue<SystemSample> _pendingSamples = new();
    private readonly Lock _gate = new();
    private bool _dirty;

    public SemaphoreSlim FlushLock { get; } = new(1, 1);

    public long DroppedObservations { get; private set; }

    public void AddRequest(RequestTrace trace)
    {
        lock (_gate)
        {
            Enqueue(_requests, trace, _options.LiveRequestCapacity);
            if (_pendingRequests.Count >= _options.PendingCapacity)
            {
                DroppedObservations++;
                return;
            }

            _pendingRequests.Enqueue(trace);
        }
    }

    public void AddSample(SystemSample sample)
    {
        lock (_gate)
        {
            Enqueue(_samples, sample, _options.LiveSampleCapacity);
            if (_pendingSamples.Count >= _options.PendingCapacity)
            {
                DroppedObservations++;
                return;
            }

            _pendingSamples.Enqueue(sample);
        }
    }

    public IReadOnlyList<RequestTrace> QueryRequests(DateTime fromUtc)
    {
        lock (_gate)
            return _requests.Where(trace => trace.OccurredAtUtc >= fromUtc).ToArray();
    }

    public IReadOnlyList<SystemSample> QuerySamples(DateTime fromUtc)
    {
        lock (_gate)
            return _samples.Where(sample => sample.CapturedAtUtc >= fromUtc).ToArray();
    }

    public IReadOnlyList<RequestTrace> PeekPendingRequests(int maxCount)
    {
        lock (_gate)
            return _pendingRequests.Take(maxCount).ToArray();
    }

    public IReadOnlyList<SystemSample> PeekPendingSamples(int maxCount)
    {
        lock (_gate)
            return _pendingSamples.Take(maxCount).ToArray();
    }

    public void AcknowledgeRequests(int count)
    {
        lock (_gate)
            Dequeue(_pendingRequests, count, "درخواست");
    }

    public void AcknowledgeSamples(int count)
    {
        lock (_gate)
            Dequeue(_pendingSamples, count, "نمونه");
    }

    public bool HasPending
    {
        get
        {
            lock (_gate)
                return _dirty || _pendingRequests.Count > 0 || _pendingSamples.Count > 0;
        }
    }

    public (IReadOnlyList<RequestTrace> Requests, IReadOnlyList<SystemSample> Samples) Capture()
    {
        lock (_gate)
            return (_requests.ToArray(), _samples.ToArray());
    }

    public void ClearPending()
    {
        lock (_gate)
        {
            _pendingRequests.Clear();
            _pendingSamples.Clear();
            _dirty = false;
        }
    }

    public void SeedRequests(IReadOnlyList<RequestTrace> traces)
    {
        lock (_gate)
        {
            foreach (var trace in traces)
                Enqueue(_requests, trace, _options.LiveRequestCapacity);
        }
    }

    public void SeedSamples(IReadOnlyList<SystemSample> samples)
    {
        lock (_gate)
        {
            foreach (var sample in samples)
                Enqueue(_samples, sample, _options.LiveSampleCapacity);
        }
    }

    public void TrimRequests(DateTime thresholdUtc)
    {
        lock (_gate)
        {
            var before = _requests.Count + _pendingRequests.Count;
            Trim(_requests, thresholdUtc, static trace => trace.OccurredAtUtc);
            Trim(_pendingRequests, thresholdUtc, static trace => trace.OccurredAtUtc);
            if (_requests.Count + _pendingRequests.Count != before)
                _dirty = true;
        }
    }

    public void TrimSamples(DateTime thresholdUtc)
    {
        lock (_gate)
        {
            var before = _samples.Count + _pendingSamples.Count;
            Trim(_samples, thresholdUtc, static sample => sample.CapturedAtUtc);
            Trim(_pendingSamples, thresholdUtc, static sample => sample.CapturedAtUtc);
            if (_samples.Count + _pendingSamples.Count != before)
                _dirty = true;
        }
    }

    private static void Enqueue<T>(Queue<T> queue, T item, int capacity)
    {
        queue.Enqueue(item);
        while (queue.Count > capacity)
            queue.Dequeue();
    }

    private static void Dequeue<T>(Queue<T> queue, int count, string name)
    {
        if (count > queue.Count)
            throw new InvalidOperationException($"تعداد تایید صف {name} از موجودی صف بیشتر است.");

        for (var index = 0; index < count; index++)
            queue.Dequeue();
    }

    private static void Trim<T>(Queue<T> queue, DateTime thresholdUtc, Func<T, DateTime> timestamp)
    {
        var kept = queue.Where(item => timestamp(item) >= thresholdUtc).ToArray();
        queue.Clear();
        foreach (var item in kept)
            queue.Enqueue(item);
    }
}
