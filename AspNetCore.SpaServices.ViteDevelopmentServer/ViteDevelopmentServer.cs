namespace AspNetCore.SpaServices.ViteDevelopmentServer;

public static class ViteDevelopmentServerMiddleware
{
    private const string logCategoryName = "ViteDevelopmentServer";

    public static void Attach(ISpaBuilder spaBuilder, string scriptName)
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
        var port = StartViteServer(sourcePath, scriptName, pkgManagerCommand, devServerPort, logger, diagnosticSource, applicationStoppingToken);

        var targetUri = new UriBuilder("http", "localhost", port).Uri;

        spaBuilder.UseProxyToSpaDevelopmentServer(targetUri);
    }

    private static int StartViteServer(
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

        ViteDevelopmentServerMiddleware.Attach(spaBuilder, npmScript);
    }
}