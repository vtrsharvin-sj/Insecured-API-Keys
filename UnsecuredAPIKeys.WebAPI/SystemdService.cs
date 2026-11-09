using System.Diagnostics;
using UnsecuredAPIKeys.WebAPI.Controllers;

namespace UnsecuredAPIKeys.WebAPI
{
    public interface ISystemdService
    {
        Task<StatusController.SystemServicesStatus> GetServicesStatusAsync();
    }

    public class SystemdService(ILogger<SystemdService> logger) : ISystemdService
    {
        public async Task<StatusController.SystemServicesStatus> GetServicesStatusAsync()
        {
            var status = new StatusController.SystemServicesStatus();

            try
            {
                status.ScraperService = await GetServiceStatusAsync(Environment.GetEnvironmentVariable("SCRAPER_SERVICE_NAME") ?? "api-scraper");
                status.VerifierService = await GetServiceStatusAsync(Environment.GetEnvironmentVariable("VERIFIER_SERVICE_NAME") ?? "api-verifier");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting service status");
                throw;
            }

            return status;
        }

        private async Task<StatusController.ServiceStatus> GetServiceStatusAsync(string serviceName)
        {
            var status = new StatusController.ServiceStatus
            {
                ServiceName = serviceName
            };

            // Prefer Docker container status (Docker Compose deployments), fall back to systemd.
            var (dockerStatus, dockerDescription, dockerLastRunTime) = await GetDockerServiceStatusAsync(serviceName);
            if (dockerStatus != null)
            {
                status.Status = dockerStatus;
                status.IsActive = dockerStatus.Contains("Up", StringComparison.OrdinalIgnoreCase);
                status.Description = dockerDescription ?? $"Docker container: {serviceName}";
                status.LastRunTime = dockerLastRunTime ?? string.Empty;
                return status;
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
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting status for service {ServiceName}", serviceName);
                status.Status = $"Error: {ex.Message}";
                status.IsActive = false;
            }

            return status;
        }

        private async Task<(string? status, string? description, string? lastRunTime)> GetDockerServiceStatusAsync(string containerName)
        {
            try
            {
                var (_, exitCode) = await RunCommandAsync("docker", "version --format '{{.Server.Version}}'", useSudo: false, timeoutMs: 5000);
                // Exit code != 0 usually means the docker daemon/socket is unavailable.
                if (exitCode != 0)
                {
                    return (null, null, null);
                }

                var (psOutput, _) = await RunCommandAsync("docker",
                    $"ps -a --filter name=^/{containerName}$ --format \"{{{{.Names}}}}|{{{{.Status}}}}\"",
                    useSudo: false, timeoutMs: 5000);

                var line = psOutput.Trim();
                if (string.IsNullOrEmpty(line))
                {
                    return ($"Not found: no container named '{containerName}'", $"Docker container: {containerName}", null);
                }

                var parts = line.Split('|');
                var containerStatus = parts.Length > 1 ? parts[1] : "unknown";

                var (startedAt, _) = await RunCommandAsync("docker",
                    $"inspect --format \"{{{{.State.StartedAt}}}}\" {containerName}",
                    useSudo: false, timeoutMs: 5000);

                return (containerStatus, $"Docker container: {containerName}", startedAt.Trim());
            }
            catch
            {
                // Docker not installed or not accessible - use systemd fallback.
                return (null, null, null);
            }
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

            var output = await process.StandardOutput.ReadToEndAsync();
            var timeoutTask = Task.Run(() =>
            {
                try
                {
                    process.WaitForExit(timeoutMs);
                }
                catch { /* process already exited or was disposed */ }
            });
            await timeoutTask.WaitAsync(TimeSpan.FromMilliseconds(timeoutMs + 1000));
            await process.WaitForExitAsync();

            return (output, process.ExitCode);
        }
    }
}
