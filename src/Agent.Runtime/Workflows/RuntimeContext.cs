using System.Collections.Concurrent;

namespace Agent.Runtime.Workflows;

public sealed class RuntimeContext
{
    private readonly ConcurrentDictionary<string, Func<object, CancellationToken, Task<object?>>> _tools = new();
    private readonly ConcurrentDictionary<string, Func<object, CancellationToken, Task<object?>>> _generators = new();

    public List<RuntimeCheckpoint> Checkpoints { get; } = [];

    public void RegisterTool(string toolName, Func<object, CancellationToken, Task<object?>> handler)
    {
        _tools[toolName] = handler;
    }

    public void RegisterGenerator(string promptCode, Func<object, CancellationToken, Task<object?>> handler)
    {
        _generators[promptCode] = handler;
    }

    public async Task<dynamic> InvokeAsync(string toolName, object input, CancellationToken cancellationToken = default)
    {
        if (!_tools.TryGetValue(toolName, out var handler))
        {
            throw new InvalidOperationException($"Tool '{toolName}' is not registered.");
        }

        return await handler(input, cancellationToken) ?? new object();
    }

    public async Task<T> InvokeAsync<T>(string toolName, object input, CancellationToken cancellationToken = default)
    {
        var result = await InvokeAsync(toolName, input, cancellationToken);
        return (T)result;
    }

    public async Task<dynamic> GenerateAsync(string promptCode, object input, CancellationToken cancellationToken = default)
    {
        if (!_generators.TryGetValue(promptCode, out var handler))
        {
            throw new InvalidOperationException($"Prompt '{promptCode}' is not registered.");
        }

        return await handler(input, cancellationToken) ?? new object();
    }

    public async Task<T> GenerateAsync<T>(string promptCode, object input, CancellationToken cancellationToken = default)
    {
        var result = await GenerateAsync(promptCode, input, cancellationToken);
        return (T)result;
    }

    public Task CreateCheckpointAsync(string checkpointType, object? value, CancellationToken cancellationToken = default)
    {
        Checkpoints.Add(new RuntimeCheckpoint(checkpointType, value));
        return Task.CompletedTask;
    }
}
