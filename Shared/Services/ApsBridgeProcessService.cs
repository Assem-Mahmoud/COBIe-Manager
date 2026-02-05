using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using COBIeManager.Shared.Interfaces;

namespace COBIeManager.Shared.Services;

/// <summary>
/// Service for managing the APS Bridge process lifecycle
/// </summary>
public class ApsBridgeProcessService : IDisposable
{
    private Process? _bridgeProcess;
    private readonly string _bridgeExecutablePath;
    private readonly IApsBridgeClient _bridgeClient;
    private DateTime _lastActivity;
    private Timer? _idleCheckTimer;
    private readonly TimeSpan _idleTimeout = TimeSpan.FromMinutes(5);
    private readonly SemaphoreSlim _startLock = new SemaphoreSlim(1, 1);
    private bool _disposed;

    public ApsBridgeProcessService(Shared.Interfaces.IApsBridgeClient bridgeClient)
    {
        _bridgeClient = bridgeClient;
        _lastActivity = DateTime.UtcNow;

        // Path to APS.Bridge.exe
        // During development, it's in the bin directory relative to the add-in
        var solutionRoot = GetSolutionRoot();
        _bridgeExecutablePath = Path.Combine(solutionRoot, "APS.Bridge", "bin", "Debug", "net8.0", "APS.Bridge.exe");

        // Fallback to release build if debug doesn't exist
        if (!File.Exists(_bridgeExecutablePath))
        {
            _bridgeExecutablePath = Path.Combine(solutionRoot, "APS.Bridge", "bin", "Release", "net8.0", "APS.Bridge.exe");
        }

        // Start idle check timer
        _idleCheckTimer = new Timer(CheckIdleTimeout, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    /// <summary>
    /// Start the bridge process if not already running
    /// </summary>
    public async Task<bool> StartBridgeAsync()
    {
        await _startLock.WaitAsync();
        try
        {
            // Check if already running
            if (IsBridgeRunning())
            {
                return true;
            }

            // Verify bridge executable exists
            if (!File.Exists(_bridgeExecutablePath))
            {
                throw new FileNotFoundException(
                    $"APS Bridge executable not found at: {_bridgeExecutablePath}",
                    _bridgeExecutablePath);
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = _bridgeExecutablePath,
                UseShellExecute = false,
                CreateNoWindow = false, // Show console for debugging
                WindowStyle = ProcessWindowStyle.Normal
            };

            _bridgeProcess = Process.Start(startInfo);

            if (_bridgeProcess == null)
            {
                return false;
            }

            // Wait for bridge to be ready (health check)
            var maxRetries = 30; // 30 seconds
            for (int i = 0; i < maxRetries; i++)
            {
                await Task.Delay(1000);
                if (await _bridgeClient.CheckHealthAsync())
                {
                    _lastActivity = DateTime.UtcNow;
                    return true;
                }
            }

            // Bridge didn't become healthy
            StopBridge();
            return false;
        }
        finally
        {
            _startLock.Release();
        }
    }

    /// <summary>
    /// Stop the bridge process gracefully
    /// </summary>
    public void StopBridge()
    {
        try
        {
            if (_bridgeProcess != null && !_bridgeProcess.HasExited)
            {
                // Try graceful shutdown first via sending Ctrl+C
                // For now, we'll just kill the process
                _bridgeProcess.Kill();
                _bridgeProcess.WaitForExit(5000);
            }
        }
        catch (Exception)
        {
            // Ignore errors during shutdown
        }
        finally
        {
            _bridgeProcess?.Dispose();
            _bridgeProcess = null;
        }
    }

    /// <summary>
    /// Stop the bridge process asynchronously
    /// </summary>
    public async Task StopBridgeAsync()
    {
        await Task.Run(() => StopBridge());
    }

    /// <summary>
    /// Check if the bridge process is running
    /// </summary>
    public bool IsBridgeRunning()
    {
        if (_bridgeProcess == null)
            return false;

        if (_bridgeProcess.HasExited)
            return false;

        // Also verify via health check
        try
        {
            return _bridgeClient.CheckHealthAsync().GetAwaiter().GetResult();
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Ensure bridge is running before an operation
    /// Auto-starts if not running
    /// </summary>
    public async Task<bool> EnsureBridgeRunningAsync()
    {
        UpdateActivity();
        return await StartBridgeAsync();
    }

    /// <summary>
    /// Update the last activity timestamp
    /// </summary>
    public void UpdateActivity()
    {
        _lastActivity = DateTime.UtcNow;
    }

    /// <summary>
    /// Check for idle timeout and stop bridge if inactive
    /// </summary>
    private void CheckIdleTimeout(object? state)
    {
        if (_disposed)
            return;

        var idleTime = DateTime.UtcNow - _lastActivity;
        if (idleTime > _idleTimeout && IsBridgeRunning())
        {
            // Stop the bridge due to inactivity
            StopBridge();
        }
    }

    /// <summary>
    /// Get the solution root directory
    /// </summary>
    private static string GetSolutionRoot()
    {
        // Start from current directory and go up until we find the .sln file
        var currentDir = Directory.GetCurrentDirectory();
        var dir = new DirectoryInfo(currentDir);

        while (dir != null)
        {
            if (dir.GetFiles("*.sln").Length > 0)
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }

        // Fallback: use the add-in's directory
        var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
        if (!string.IsNullOrEmpty(assemblyLocation))
        {
            return Path.GetDirectoryName(assemblyLocation) ?? currentDir;
        }

        return currentDir;
    }

    /// <summary>
    /// Get the bridge executable path (for diagnostic purposes)
    /// </summary>
    public string GetBridgeExecutablePath() => _bridgeExecutablePath;

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        _idleCheckTimer?.Dispose();
        StopBridge();
        _startLock.Dispose();

        // Dispose the bridge client if it implements IDisposable
        if (_bridgeClient is IDisposable disposableClient)
        {
            disposableClient.Dispose();
        }
    }
}
