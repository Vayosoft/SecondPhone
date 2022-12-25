using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using Benchmarks;

var job = Job.Default;
IConfig config = DefaultConfig.Instance
    .AddDiagnoser(MemoryDiagnoser.Default);

if (args.Length == 1 && args[0] == "inprocess")
{
    job = job
        .WithMinWarmupCount(2)
        .WithMaxWarmupCount(4)
        .WithToolchain(InProcessEmitToolchain.Instance);
    config = config.WithOptions(ConfigOptions.DisableOptimizationsValidator);
}
else
{
    if (args.Length > 0)
    {
        string artifactsPath = args[0];
        Directory.CreateDirectory(artifactsPath);
        config = config.WithArtifactsPath(artifactsPath);
    }

    if (args.Length > 1)
    {
        string version = args[1];
        job = job.WithArguments(new Argument[] { new MsBuildArgument($"/p:Version={version}") });
    }
}

config = config.AddJob(job);

BenchmarkRunner.Run<HandshakeBenchmarks>(config);

