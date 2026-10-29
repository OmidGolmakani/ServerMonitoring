using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.Extensions.Options;
using Microsoft.Web.Administration;
using ServerMonitoring.Application.Options;

namespace ServerMonitoring.Infrastructure.Monitoring;

public sealed class IisRuntimeSiteDiscoverer(IOptions<MonitoringOptions> options)
{
    private static readonly Regex AppPoolFromW3wp = new(
        @"^(?<pid>\d+)_(?<pool>.+)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly MonitoringOptions _options = options.Value;

    public IisDiscoveryResult Discover()
    {
        var map = new Dictionary<string, DiscoveredSite>(StringComparer.OrdinalIgnoreCase);
        var notes = new List<string>();
        var configPath = Environment.ExpandEnvironmentVariables(_options.IisApplicationHostConfigPath);
        var configExists = false;
        var configReadable = false;

        TryDiscoverWithServerManager(map, notes);
        TryDiscoverWithAppCmd(map, notes);
        TryDiscoverFromConfigFile(configPath, map, notes, ref configExists, ref configReadable);
        TryDiscoverFromWebServiceCounters(map, notes);
        TryDiscoverRunningAppPools(map, notes);

        return new IisDiscoveryResult(
            map.Values.ToArray(),
            notes,
            configPath,
            configExists,
            configReadable);
    }

    private static void TryDiscoverWithServerManager(
        IDictionary<string, DiscoveredSite> map,
        ICollection<string> notes)
    {
        try
        {
            using var manager = new ServerManager();
            var countBefore = map.Count;
            foreach (var site in manager.Sites)
            {
                foreach (var binding in site.Bindings)
                {
                    if (!IsHttpBinding(binding.Protocol))
                        continue;

                    var url = BuildUrl(binding.Protocol, binding.Host, binding.EndPoint?.Port);
                    if (url is null)
                        continue;

                    Add(map, site.Name, url, "iis-api");
                }
            }

            notes.Add(map.Count > countBefore
                ? "کشف از Microsoft.Web.Administration موفق بود."
                : "ServerManager اجرا شد ولی binding دامنه برنگرداند.");
        }
        catch (Exception exception)
        {
            notes.Add($"ServerManager: {exception.Message}");
        }
    }

    private static void TryDiscoverWithAppCmd(
        IDictionary<string, DiscoveredSite> map,
        ICollection<string> notes)
    {
        var appcmd = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.System),
            "inetsrv",
            "appcmd.exe");

        if (!File.Exists(appcmd))
        {
            notes.Add("appcmd.exe پیدا نشد.");
            return;
        }

        try
        {
            var start = new ProcessStartInfo
            {
                FileName = appcmd,
                Arguments = "list sites /xml",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(start);
            if (process is null)
            {
                notes.Add("اجرای appcmd ناموفق بود.");
                return;
            }

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit(15_000);

            if (process.ExitCode != 0)
            {
                notes.Add($"appcmd: {(string.IsNullOrWhiteSpace(error) ? $"exit {process.ExitCode}" : error.Trim())}");
                return;
            }

            var document = XDocument.Parse(output);
            var countBefore = map.Count;
            foreach (var site in document.Descendants("SITE"))
            {
                var name = (string?)site.Attribute("SITE.NAME") ?? (string?)site.Attribute("site.name");
                var bindings = (string?)site.Attribute("bindings") ?? (string?)site.Attribute("BINDINGS");
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(bindings))
                    continue;

                foreach (var binding in bindings.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    // https/*:443:example.com
                    var parts = binding.Split('/');
                    if (parts.Length != 2)
                        continue;

                    var protocol = parts[0];
                    var info = parts[1].Split(':');
                    if (info.Length < 3 || !IsHttpBinding(protocol))
                        continue;

                    var portText = info[^2];
                    var host = info[^1];
                    if (!int.TryParse(portText, out var port))
                        continue;

                    var url = BuildUrl(protocol, host, port);
                    if (url is null)
                        continue;

                    Add(map, name, url, "appcmd");
                }
            }

            notes.Add(map.Count > countBefore
                ? "کشف از appcmd موفق بود."
                : "appcmd اجرا شد ولی دامنه برنگرداند.");
        }
        catch (Exception exception)
        {
            notes.Add($"appcmd: {exception.Message}");
        }
    }

    private void TryDiscoverFromConfigFile(
        string path,
        IDictionary<string, DiscoveredSite> map,
        ICollection<string> notes,
        ref bool exists,
        ref bool readable)
    {
        try
        {
            exists = File.Exists(path);
            if (!exists)
            {
                notes.Add($"فایل پیکربندی IIS پیدا نشد: {path}");
                return;
            }

            using var stream = File.OpenRead(path);
            readable = true;
            var document = XDocument.Load(stream);
            var countBefore = map.Count;
            foreach (var site in ParseConfigSites(document))
                Add(map, site.Name, site.Url, "iis-config");

            notes.Add(map.Count > countBefore
                ? "کشف از applicationHost.config موفق بود."
                : "applicationHost.config خوانده شد ولی دامنه برنگرداند.");
        }
        catch (Exception exception)
        {
            notes.Add($"applicationHost.config: {exception.Message}");
        }
    }

    private static void TryDiscoverFromWebServiceCounters(
        IDictionary<string, DiscoveredSite> map,
        ICollection<string> notes)
    {
        try
        {
            if (!PerformanceCounterCategory.Exists("Web Service"))
            {
                notes.Add("Performance Counter «Web Service» موجود نیست.");
                return;
            }

            var category = new PerformanceCounterCategory("Web Service");
            var countBefore = map.Count;
            foreach (var instance in category.GetInstanceNames())
            {
                if (string.Equals(instance, "_Total", StringComparison.OrdinalIgnoreCase))
                    continue;

                foreach (var url in GuessUrlsFromSiteName(instance))
                    Add(map, instance, url, "iis-counter");
            }

            notes.Add(map.Count > countBefore
                ? "کشف از Performance Counter وب‌سرویس موفق بود."
                : "Performance Counter وب‌سرویس فقط _Total داشت.");
        }
        catch (Exception exception)
        {
            notes.Add($"Web Service counter: {exception.Message}");
        }
    }

    private static void TryDiscoverRunningAppPools(
        IDictionary<string, DiscoveredSite> map,
        ICollection<string> notes)
    {
        var pools = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            if (PerformanceCounterCategory.Exists("W3SVC_W3WP"))
            {
                var category = new PerformanceCounterCategory("W3SVC_W3WP");
                foreach (var instance in category.GetInstanceNames())
                {
                    if (string.Equals(instance, "_Total", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var match = AppPoolFromW3wp.Match(instance);
                    if (match.Success)
                        pools.Add(match.Groups["pool"].Value);
                }
            }
        }
        catch (Exception exception)
        {
            notes.Add($"W3SVC_W3WP: {exception.Message}");
        }

        try
        {
            if (PerformanceCounterCategory.Exists("APP_POOL_WAS"))
            {
                var category = new PerformanceCounterCategory("APP_POOL_WAS");
                foreach (var instance in category.GetInstanceNames())
                {
                    if (string.Equals(instance, "_Total", StringComparison.OrdinalIgnoreCase))
                        continue;

                    pools.Add(instance);
                }
            }
        }
        catch (Exception exception)
        {
            notes.Add($"APP_POOL_WAS: {exception.Message}");
        }

        if (pools.Count == 0)
        {
            notes.Add("process/w3wp فعالی برای App Pool پیدا نشد.");
            return;
        }

        notes.Add("App Poolهای در حال اجرا از w3wp/Performance Counter: " + string.Join("، ", pools));
    }

    private static IEnumerable<DiscoveredSite> ParseConfigSites(XDocument document)
    {
        var sites = document.Root
            ?.Element("system.applicationHost")
            ?.Element("sites")
            ?.Elements("site");

        if (sites is null)
            yield break;

        foreach (var site in sites)
        {
            var name = (string?)site.Attribute("name");
            if (string.IsNullOrWhiteSpace(name))
                continue;

            foreach (var binding in site.Element("bindings")?.Elements("binding") ?? [])
            {
                var protocol = (string?)binding.Attribute("protocol");
                var information = (string?)binding.Attribute("bindingInformation");
                if (!IsHttpBinding(protocol) || string.IsNullOrWhiteSpace(information))
                    continue;

                var parts = information.Split(':');
                if (parts.Length < 3 || !int.TryParse(parts[^2], out var port))
                    continue;

                var url = BuildUrl(protocol!, parts[^1], port);
                if (url is null)
                    continue;

                yield return new DiscoveredSite(name, url, "iis-config");
            }
        }
    }

    private static IEnumerable<string> GuessUrlsFromSiteName(string siteName)
    {
        if (string.Equals(siteName, "Default Web Site", StringComparison.OrdinalIgnoreCase))
        {
            yield return "http://localhost/";
            yield return "https://localhost/";
            yield break;
        }

        if (siteName.Contains('.', StringComparison.Ordinal) ||
            string.Equals(siteName, "localhost", StringComparison.OrdinalIgnoreCase))
        {
            yield return $"http://{siteName}/";
            yield return $"https://{siteName}/";
            yield break;
        }

        // نام سایت دامنه نیست؛ فقط روی localhost با نام سایت نگه می‌داریم.
        yield return "http://localhost/";
    }

    private static string? BuildUrl(string protocol, string? host, int? port)
    {
        if (!IsHttpBinding(protocol) || port is null or <= 0)
            return null;

        var normalizedHost = string.IsNullOrWhiteSpace(host) || host == "*"
            ? "localhost"
            : host.Trim();

        if (normalizedHost.Contains('*'))
            normalizedHost = "localhost";

        var defaultPort = string.Equals(protocol, "https", StringComparison.OrdinalIgnoreCase) ? 443 : 80;
        return port == defaultPort
            ? $"{protocol}://{normalizedHost}/"
            : $"{protocol}://{normalizedHost}:{port}/";
    }

    private static bool IsHttpBinding(string? protocol)
        => string.Equals(protocol, "http", StringComparison.OrdinalIgnoreCase)
           || string.Equals(protocol, "https", StringComparison.OrdinalIgnoreCase);

    private static void Add(
        IDictionary<string, DiscoveredSite> map,
        string name,
        string url,
        string source)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return;

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            return;

        if (map.ContainsKey(uri.AbsoluteUri))
            return;

        map[uri.AbsoluteUri] = new DiscoveredSite(name.Trim(), uri.AbsoluteUri, source);
    }

    public sealed record DiscoveredSite(string Name, string Url, string Source);

    public sealed record IisDiscoveryResult(
        IReadOnlyList<DiscoveredSite> Sites,
        IReadOnlyList<string> Notes,
        string ConfigPath,
        bool ConfigExists,
        bool ConfigReadable);
}
