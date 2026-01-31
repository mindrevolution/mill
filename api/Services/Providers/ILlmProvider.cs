using System.Diagnostics;
using MillApi.Models;

namespace MillApi.Services.Providers;

/// <summary>
/// Interface for LLM providers that can execute prompts.
/// </summary>
public interface ILlmProvider
{
    /// <summary>
    /// Provider name (e.g., "claude", "opencode")
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Execute a prompt and return the response.
    /// </summary>
    Task<LlmResponse> Execute(string prompt, LlmOptions options);

    /// <summary>
    /// Spawn an interactive process for TTY-based interaction.
    /// </summary>
    Process SpawnInteractive(string prompt, string? workingDir = null);

    /// <summary>
    /// Check if the provider is available (CLI installed and accessible).
    /// </summary>
    Task<bool> IsAvailable();
}
