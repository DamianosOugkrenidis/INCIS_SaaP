// INCIS SaaP — τοπικό benchmark harness (απομονωμένο, no production).
// - HTTP latency/throughput: in-process TestServer + InMemory DB (δεν αγγίζει πραγματική DB).
// - SQL / INCI import / ML: απομονωμένη τοπική DB SQLEXPRESS02\INCIS_SaaP_bench + πραγματικό μοντέλο.
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using INCIS_SaaP.Data;
using INCIS_SaaP.ML.FormulaIntelligence;
using INCIS_SaaP.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Hangfire;

namespace INCIS_SaaP.Bench;

internal static class BenchRunner
{
    private const string BenchSql = @"Server=.\SQLEXPRESS02;Database=INCIS_SaaP_bench;Trusted_Connection=True;TrustServerCertificate=True;Max Pool Size=100;";
    private const string Root = @"C:\Users\daoug\Desktop\ΠΡΟΣ_ΠΑΡΑΔΟΣΗ_ΙΩΑΝΝΟΥ\INCIS_FINAL\INCIS_SaaP_v5";

    private static double Percentile(List<double> xs, double p)
    {
        if (xs.Count == 0) return 0;
        var s = xs.OrderBy(x => x).ToList();
        var idx = (int)Math.Ceiling(p / 100.0 * s.Count) - 1;
        return s[Math.Clamp(idx, 0, s.Count - 1)];
    }

    public static async Task<int> Main()
    {
        var inciFile = Path.Combine(Root, "ALL_INCIS_NEW_FILE.xls");
        var results = new Dictionary<string, object?>();
        Console.WriteLine("=== INCIS SaaP benchmark harness ===");
        Console.WriteLine($"dotnet {Environment.Version} | {DateTime.UtcNow:O}");

        // ---------- 1) HTTP latency & throughput (in-process TestServer) ----------
        try
        {
            using var factory = new BenchFactory();
            var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            var http = new Dictionary<string, object>();
            foreach (var ep in new[] { "/health", "/Account/Login" })
            {
                for (int i = 0; i < 20; i++) await client.GetAsync(ep);
                var lat = new List<double>();
                var codes = new Dictionary<int, int>();
                for (int i = 0; i < 300; i++)
                {
                    var sw = Stopwatch.StartNew();
                    var r = await client.GetAsync(ep);
                    sw.Stop();
                    lat.Add(sw.Elapsed.TotalMilliseconds);
                    var c = (int)r.StatusCode; codes[c] = codes.TryGetValue(c, out var v) ? v + 1 : 1;
                }
                http[ep] = new { count = lat.Count, mean_ms = Math.Round(lat.Average(), 3), p50_ms = Math.Round(Percentile(lat, 50), 3), p95_ms = Math.Round(Percentile(lat, 95), 3), p99_ms = Math.Round(Percentile(lat, 99), 3), min_ms = Math.Round(lat.Min(), 3), max_ms = Math.Round(lat.Max(), 3), status_counts = codes };
                Console.WriteLine($"[HTTP] {ep} p50={Percentile(lat, 50):F2}ms p95={Percentile(lat, 95):F2}ms p99={Percentile(lat, 99):F2}ms");
            }
            results["http_latency"] = http;

            const int workers = 10, perWorker = 30;
            var all = new List<double>();
            var failed = 0;
            var swT = Stopwatch.StartNew();
            await Parallel.ForEachAsync(Enumerable.Range(0, workers), new ParallelOptions { MaxDegreeOfParallelism = workers }, async (_, ct) =>
            {
                for (int i = 0; i < perWorker; i++)
                {
                    var s = Stopwatch.StartNew();
                    var r = await client.GetAsync("/health", ct);
                    s.Stop();
                    lock (all) { all.Add(s.Elapsed.TotalMilliseconds); if (r.StatusCode != HttpStatusCode.OK) failed++; }
                }
            });
            swT.Stop();
            var total = workers * perWorker;
            var rps = total / swT.Elapsed.TotalSeconds;
            results["throughput"] = new { workers, per_worker = perWorker, total, seconds = Math.Round(swT.Elapsed.TotalSeconds, 3), rps = Math.Round(rps, 1), failed, p95_ms = Math.Round(Percentile(all, 95), 3) };
            Console.WriteLine($"[THROUGHPUT] {total} reqs, {workers} parallel, {rps:F1} req/s, failed={failed}, p95={Percentile(all, 95):F2}ms");
        }
        catch (Exception ex) { results["http_error"] = ex.Message; Console.WriteLine("[HTTP] ERROR: " + ex.Message); }

        // ---------- 2) SQL query perf + INCI import (isolated DB) ----------
        try
        {
            var opts = new DbContextOptionsBuilder<INCISDbContext>().UseSqlServer(BenchSql).Options;
            using var db = new INCISDbContext(opts, null!);
            db.Database.EnsureDeleted();
            var swM = Stopwatch.StartNew();
            db.Database.Migrate();
            swM.Stop();
            var migrationsApplied = db.Database.GetAppliedMigrations().Count();
            Console.WriteLine($"[SQL] migrations applied={migrationsApplied} in {swM.Elapsed.TotalSeconds:F2}s");
            results["sql_migrations_applied"] = migrationsApplied;
            results["sql_migrate_ms"] = Math.Round(swM.Elapsed.TotalMilliseconds, 1);

            var importer = new InciCatalogImportService(db, NullLogger<InciCatalogImportService>.Instance);
            var swI = Stopwatch.StartNew();
            var imp = await importer.ImportFromFileAsync(inciFile);
            swI.Stop();
            Console.WriteLine($"[INCI] import total={imp.TotalDataRows} inserted={imp.Inserted} updated={imp.Updated} ms={swI.Elapsed.TotalMilliseconds:F0}");
            var swI2 = Stopwatch.StartNew();
            var imp2 = await importer.ImportFromFileAsync(inciFile);
            swI2.Stop();
            results["inci_import"] = new { source = Path.GetFileName(inciFile), total_rows = imp.TotalDataRows, inserted = imp.Inserted, updated = imp.Updated, first_run_ms = Math.Round(swI.Elapsed.TotalMilliseconds, 1), second_run_inserted = imp2.Inserted, second_run_ms = Math.Round(swI2.Elapsed.TotalMilliseconds, 1), fatal = imp.FatalError };

            var q = new Dictionary<string, object>();
            async Task Time(string name, Func<Task<object?>> f)
            {
                await f();
                var list = new List<double>();
                for (int i = 0; i < 20; i++) { var s = Stopwatch.StartNew(); await f(); s.Stop(); list.Add(s.Elapsed.TotalMilliseconds); }
                q[name] = new { p50_ms = Math.Round(Percentile(list, 50), 3), p95_ms = Math.Round(Percentile(list, 95), 3) };
                Console.WriteLine($"[SQL] {name} p50={Percentile(list, 50):F2}ms p95={Percentile(list, 95):F2}ms");
            }
            await Time("count_incis", async () => await db.INCIs.LongCountAsync());
            await Time("search_incis_top50", async () => await db.INCIs.Where(x => x.Name!.Contains("acid")).OrderBy(x => x.Name).Take(50).ToListAsync());
            await Time("search_by_cas", async () => await db.INCIs.Where(x => x.CASNumber == "7732-18-5").Take(10).ToListAsync());
            results["sql_query_perf"] = q;
        }
        catch (Exception ex) { results["sql_error"] = ex.Message; Console.WriteLine("[SQL] ERROR: " + ex.Message); }

        // ---------- 3) ML inference latency (real persisted model) ----------
        try
        {
            var env = new FakeEnv { ContentRootPath = Path.Combine(Root, "INCIS_SaaP") };
            var sp = new ServiceCollection().BuildServiceProvider();
            var model = new FormulaSuccessModel(sp, env, NullLogger<FormulaSuccessModel>.Instance);
            var swT = Stopwatch.StartNew();
            var meta = await model.EnsureTrainedAsync();
            swT.Stop();
            Console.WriteLine($"[ML] trained source={meta.TrainingSource} samples={meta.SampleCount} in {swT.Elapsed.TotalMilliseconds:F0}ms");
            for (int i = 0; i < 100; i++) model.Predict(new FormulaMlFeatures { RawMaterialCount = 12, WaterPercentage = 65, TotalPercentage = 100, ProductType = "Leave on" });
            var lat = new List<double>();
            for (int i = 0; i < 2000; i++)
            {
                var f = new FormulaMlFeatures { RawMaterialCount = 8 + (i % 20), WaterPercentage = 20 + (i % 70), TotalPercentage = 100, PreservativeCount = i % 4, HasPreservatives = i % 2, IsAnhydrous = i % 2, IsExternalDevelopment = i % 3 == 0 ? 1 : 0, ProductType = i % 2 == 0 ? "Leave on" : "Rinse off" };
                var s = Stopwatch.StartNew();
                model.Predict(f);
                s.Stop();
                lat.Add(s.Elapsed.TotalMicroseconds);
            }
            results["ml_inference"] = new { training_source = meta.TrainingSource, sample_count = meta.SampleCount, accuracy = Math.Round(meta.Accuracy, 4), auc = Math.Round(meta.AreaUnderRocCurve, 4), f1 = Math.Round(meta.F1Score, 4), predictions = lat.Count, mean_us = Math.Round(lat.Average(), 2), p50_us = Math.Round(Percentile(lat, 50), 2), p95_us = Math.Round(Percentile(lat, 95), 2), p99_us = Math.Round(Percentile(lat, 99), 2) };
            Console.WriteLine($"[ML] predict mean={lat.Average():F1}us p95={Percentile(lat, 95):F1}us");
        }
        catch (Exception ex) { results["ml_error"] = ex.Message; Console.WriteLine("[ML] ERROR: " + ex.Message); }

        // ---------- 4) memory ----------
        try
        {
            var p = Process.GetCurrentProcess();
            p.Refresh();
            results["memory"] = new { working_set_mb = Math.Round(p.WorkingSet64 / 1024.0 / 1024.0, 1), gc_total_mb = Math.Round(GC.GetTotalMemory(false) / 1024.0 / 1024.0, 1), gc_heap_mb = Math.Round(GC.GetGCMemoryInfo().HeapSizeBytes / 1024.0 / 1024.0, 1) };
            Console.WriteLine($"[MEM] working_set={p.WorkingSet64 / 1024.0 / 1024.0:F1}MB");
        }
        catch { }

        var outDir = Path.Combine(Root, "bench", "results");
        Directory.CreateDirectory(outDir);
        var json = JsonSerializer.Serialize(new { generated_utc = DateTime.UtcNow.ToString("O"), env = Environment.Version.ToString(), results }, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(outDir, "benchmark_results.json"), json, new UTF8Encoding(false));
        Console.WriteLine("=== DONE ===");
        return 0;
    }
}

internal sealed class BenchFactory : WebApplicationFactory<Program>
{
    private readonly string _db = "BENCH_" + Guid.NewGuid();
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("DisableBackgroundServices", "true");
        builder.UseSetting("Cache:UseSqlServer", "false");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(IGlobalConfiguration));
            var ef = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<INCISDbContext>) ||
                d.ServiceType == typeof(DbContextOptions) ||
                d.ServiceType == typeof(INCISDbContext) ||
                (d.ServiceType.Namespace?.StartsWith("Microsoft.EntityFrameworkCore") ?? false) ||
                (d.ImplementationType?.Namespace?.StartsWith("Microsoft.EntityFrameworkCore") ?? false)).ToList();
            foreach (var d in ef) services.Remove(d);
            services.AddDbContext<INCISDbContext>(o => o.UseInMemoryDatabase(_db));
        });
    }
}

internal sealed class FakeEnv : IWebHostEnvironment
{
    public string ApplicationName { get; set; } = "INCIS_SaaP.Bench";
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    public string WebRootPath { get; set; } = "";
    public string EnvironmentName { get; set; } = "Development";
    public string ContentRootPath { get; set; } = "";
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
