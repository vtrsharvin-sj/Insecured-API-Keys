using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using UnsecuredAPIKeys.WebAPI.Controllers;

namespace UnsecuredAPIKeys.WebAPI
{
    public interface ISystemdService
    {
        Task<StatusController.SystemServicesStatus> GetServicesStatusAsync(bool includeLogs = false, int logLines = 50);
    }

    public class SystemdService(ILogger<SystemdService> logger) : ISystemdService
    {
        private const string DockerSocketPath = "/var/run/docker.sock";

        public async Task<StatusController.SystemServicesStatus> GetServicesStatusAsync(bool includeLogs = false, int logLines = 50)
        {
            var status = new StatusController.SystemServicesStatus();

            try
            {
                status.ScraperService = await GetServiceStatusAsync(
                    Environment.GetEnvironmentVariable("SCRAPER_SERVICE_NAME") ?? "api-scraper", includeLogs, logLines);
                status.VerifierService = await GetServiceStatusAsync(
                    Environment.GetEnvironmentVariable("VERIFIER_SERVICE_NAME") ?? "api-verifier", includeLogs, logLines);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting service status");
                throw;
            }

            return status;
        }

        private async Task<StatusController.ServiceStatus> GetServiceStatusAsync(string serviceName, bool includeLogs = false, int logLines = 50)
        {
            var status = new StatusController.ServiceStatus
            {
                ServiceName = serviceName
            };

            // Prefer the Docker Engine API (containers, works inside containers with the
            // docker socket mounted), then systemd (bare-metal deployments).
            var dockerStatus = await GetDockerServiceStatusAsync(serviceName);
            if (dockerStatus != null)
            {
                return dockerStatus;
            }

            try
            {
                var (output, _) = await RunCommandAsync("systemctl", $"status {serviceName}.service --no-pager".Trim(), useSudo: true);

                // Parse the output
                var lines = output.Split('\n');
                foreach (var line in lines)
                {
                    if (line.Contains("Active:"))
                    {
                        status.Status = line.Trim();
                        status.IsActive = line.Contains("active (running)");
                    }
                    else if (line.Contains("Description:"))
                    {
                        status.Description = line.Replace("Description:", "").Trim();
                    }
                }

                // Get the last run time from journal
                var (journalOutput, _) = await RunCommandAsync("journalctl", $"-u {serviceName}.service --no-pager -n 1 --output=short-iso", useSudo: true);

                if (!string.IsNullOrEmpty(journalOutput))
                {
                    status.LastRunTime = journalOutput.Split(' ')[0] ?? string.Empty;
                }

                // If logs are requested, get them
                if (includeLogs)
                {
                    status.RecentLogs = await GetServiceLogsAsync(serviceName, logLines);
                    if (status.RecentLogs.Count == 0)
                    {
                        status.RecentLogs = await GetServiceLogsSimpleAsync(serviceName, logLines);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting status for service {ServiceName}", serviceName);
                status.Status = $"Error: {ex.Message}";
                status.IsActive = false;
            }

            return status;
        }

        private async Task<StatusController.ServiceStatus?> GetDockerServiceStatusAsync(string containerName)
        {
            try
            {
                if (!File.Exists(DockerSocketPath))
                {
                    return null;
                }

                using var client = CreateDockerSocketClient();
                var response = await client.GetAsync($"http://dockerhost/containers/{Uri.EscapeDataString(containerName)}/json");
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                await using var stream = await response.Content.ReadAsStreamAsync();
                using var doc = await JsonDocument.ParseAsync(stream);

                var root = doc.RootElement;
                var stateObj = root.GetProperty("State");
                var isRunning = stateObj.TryGetProperty("Running", out var running) && running.GetBoolean();
                var statusText = stateObj.TryGetProperty("Status", out var statusEl) ? statusEl.GetString() ?? string.Empty : string.Empty;
                var lastRunTime = stateObj.TryGetProperty("StartedAt", out var startedEl) ? startedEl.GetString() ?? string.Empty : string.Empty;

                return new StatusController.ServiceStatus
                {
                    ServiceName = containerName,
                    Status = statusText,
                    IsActive = isRunning,
                    Description = $"Docker container: {containerName}",
                    LastRunTime = lastRunTime
                };
            }
            catch (Exception ex)
            {
                // Docker socket unavailable (bare-metal systemd deployment) - not an error.
                logger.LogDebug(ex, "Docker status unavailable for {Container}, falling back to systemd", containerName);
                return null;
            }
        }

        private static HttpClient CreateDockerSocketClient()
        {
            var handler = new SocketsHttpHandler();
            handler.ConnectCallback = async (context, cancellationToken) =>
            {
                var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                try
                {
                    await socket.ConnectAsync(new UnixDomainSocketEndPoint(DockerSocketPath), cancellationToken);
                    return new NetworkStream(socket, ownsSocket: true);
                }
                catch
                {
                    socket.Dispose();
                    throw;
                }
            };
            return new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };
        }

        private async Task<(string output, int exitCode)> RunCommandAsync(string fileName, string arguments, bool useSudo = false, int timeoutMs = 15000)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = useSudo ? "sudo" : fileName,
                Arguments = useSudo ? $"{fileName} {arguments}" : arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };

            if (process.Start() == false)
            {
                return (string.Empty, -1);
            }

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var timeoutTask = Task.Run(() =>
            {
                try
                {
                    process.WaitForExit(timeoutMs);
                }
                catch { /* process already exited or was disposed */ }
            });
            await timeoutTask.WaitAsync(TimeSpan.FromMilliseconds(timeoutMs + 1000));
            var output = await outputTask;

            return (output, process.ExitCode);
        }

        private async Task<List<StatusController.LogEntry>> GetServiceLogsAsync(string serviceName, int lines)
        {
            var logs = new List<StatusController.LogEntry>();
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "sudo",
                    Arguments = $"journalctl -u {serviceName}.service --no-pager -n {lines} --output=json",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = startInfo };
                process.Start();

                var errorOutput = await process.StandardError.ReadToEndAsync();
                if (!string.IsNullOrEmpty(errorOutput))
                {
                    logger.LogWarning("Error from journalctl: {Error}", errorOutput);
                }

                using var reader = process.StandardOutput;
                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            var logEntry = JsonSerializer.Deserialize<StatusController.LogEntry>(line);
                            if (logEntry != null)
                            {
                                logs.Add(logEntry);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning("Failed to parse log entry: {Line}, Error: {Error}", line, ex.Message);
                        logs.Add(new StatusController.LogEntry { MESSAGE = $"[Parse Error] {line}" });
                    }
                }

                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    logger.LogWarning("journalctl exited with code {ExitCode}", process.ExitCode);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting logs for service {ServiceName}", serviceName);
            }

            return logs;
        }

        // Fallback that gets logs in a simpler format
        private async Task<List<StatusController.LogEntry>> GetServiceLogsSimpleAsync(string serviceName, int lines)
        {
            var logs = new List<StatusController.LogEntry>();
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "sudo",
                    Arguments = $"journalctl -u {serviceName}.service --no-pager -n {lines}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = startInfo };
                process.Start();

                var output = await process.StandardOutput.ReadToEndAsync();

                if (!string.IsNullOrEmpty(output))
                {
                    foreach (var line in output.Split('\n'))
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            logs.Add(new StatusController.LogEntry { MESSAGE = line.Trim() });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting logs for service {ServiceName}", serviceName);
            }

            return logs;
        }
    }
}
