namespace AspNetCore.SpaServices.ViteDevelopmentServer;

public static class ViteDevelopmentServerMiddleware
{
    private const string logCategoryName = "ViteDevelopmentServer";
    private static readonly TimeSpan regexMatchTimeout = TimeSpan.FromSeconds(5);// This is a development-time only feature, so a very long timeout is fine

    public static async Task Attach(ISpaBuilder spaBuilder, string scriptName)
    {
        var pkgManagerCommand = spaBuilder.Options.PackageManagerCommand;
        var sourcePath = spaBuilder.Options.SourcePath;
        var devServerPort = spaBuilder.Options.DevServerPort;
        if (string.IsNullOrEmpty(sourcePath))
            throw new ArgumentException("Cannot be null or empty", nameof(sourcePath));

        if (string.IsNullOrEmpty(scriptName))
            throw new ArgumentException("Cannot be null or empty", nameof(scriptName));

        // Start Vite and attach to middleware pipeline
        var appBuilder = spaBuilder.ApplicationBuilder;
        var applicationStoppingToken = appBuilder.ApplicationServices.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping;
        var logger = LoggerFinder.GetOrCreateLogger(appBuilder, logCategoryName);
        var diagnosticSource = appBuilder.ApplicationServices.GetRequiredService<DiagnosticSource>();
        var portTask = StartViteServerAsync(sourcePath, scriptName, pkgManagerCommand, devServerPort, logger, diagnosticSource, applicationStoppingToken);

        var targetUriTask = portTask.ContinueWith(task => new UriBuilder("http", "localhost", task.Result).Uri);

        var timeout = spaBuilder.Options.StartupTimeout;

        // Everything we proxy is hardcoded to target http://localhost because:
        // - the requests are always from the local machine (we're not accepting remote
        //   requests that go directly to the Vite server)
        // - given that, there's no reason to use https, and we couldn't even if we
        //   wanted to, because in general the Vite server has no certificate
        spaBuilder.UseProxyToSpaDevelopmentServer(await
            targetUriTask
            .WithTimeout(timeout, "The Vite server did not start listening for requests " +
                                                    $"within the timeout period of {timeout.TotalSeconds} seconds. " +
                                                    "Check the log output for error information.")
            );
    }

    private static async Task<int> StartViteServerAsync(
        string sourcePath, string scriptName, string pkgManagerCommand, int portNumber, ILogger logger, DiagnosticSource diagnosticSource, CancellationToken applicationStoppingToken)
    {
        if (portNumber == default(int))
            portNumber = TcpPortFinder.FindAvailablePort();

        logger.LogInformation($"Starting Vite server on port {portNumber}...");

        var envVars = new Dictionary<string, string>
            {
                { "PORT", portNumber.ToString() },
                { "BROWSER", "none" }, // We don't want Vite to open its own extra browser window pointing to the internal dev server port
            };

        var scriptRunner =
            new NodeScriptRunner(sourcePath, scriptName, null, envVars, pkgManagerCommand, diagnosticSource, applicationStoppingToken);
        scriptRunner.AttachToLogger(logger);

        using (var stdErrReader = new EventedStreamStringReader(scriptRunner.StdOut))
        {
            try
            {
                // Although the React dev server may eventually tell us the URL it's listening on,
                // it doesn't do so until it's finished compiling, and even then only if there were
                // no compiler warnings. So instead of waiting for that, consider it ready as soon
                // as it starts listening for requests.
                await scriptRunner.StdOut.WaitForMatch(
                    new Regex("dev server running at", RegexOptions.None, regexMatchTimeout));
            }
            catch (EndOfStreamException ex)
            {
                throw new InvalidOperationException(
                    $"The NPM script '{scriptName}' exited without indicating that the " +
                    $"Vite server was listening for requests. The error output was: " +
                    $"{stdErrReader.ReadAsString()}", ex);
            }
        }

        return portNumber;
    }
}

/// <summary>
/// Extension methods for enabling Vite development server middleware support.
/// </summary>
public static class ViteDevelopmentServerMiddlewareExtensions
{
    /// <summary>
    /// Handles requests by passing them through to an instance of the Vite server.
    /// This means you can always serve up-to-date CLI-built resources without having
    /// to run the Vite server manually.
    ///
    /// This feature should only be used in development. For production deployments, be
    /// sure not to enable the Vite server.
    /// </summary>
    /// <param name="spaBuilder">The <see cref="ISpaBuilder"/>.</param>
    /// <param name="npmScript">The name of the script in your package.json file that launches the Vite server.</param>
    public static void UseViteDevelopmentServer(
        this ISpaBuilder spaBuilder,
        string npmScript)
    {
        ArgumentNullException.ThrowIfNull(spaBuilder, nameof(spaBuilder));

        var spaOptions = spaBuilder.Options;

        if (string.IsNullOrEmpty(spaOptions.SourcePath))
            throw new InvalidOperationException(
                $"To use {nameof(UseViteDevelopmentServer)}, you must supply a non-empty value for the {nameof(SpaOptions.SourcePath)} property of {nameof(SpaOptions)} when calling {nameof(SpaApplicationBuilderExtensions.UseSpa)}.");

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        ViteDevelopmentServerMiddleware.Attach(spaBuilder, npmScript);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
    }
}