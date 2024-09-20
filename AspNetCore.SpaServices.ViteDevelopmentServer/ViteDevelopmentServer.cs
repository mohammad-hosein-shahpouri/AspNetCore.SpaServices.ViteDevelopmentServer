using AspNetCore.SpaServices.ViteDevelopmentServer.Enums;
using AspNetCore.SpaServices.ViteDevelopmentServer.Interfaces;

namespace AspNetCore.SpaServices.ViteDevelopmentServer;

public static class ViteDevelopmentServerMiddleware
{
    private const string logCategoryName = "ViteDevelopmentServer";

    public static void Attach(ISpaBuilder spaBuilder, string scriptName, JsRuntime runtime, bool useHttps)
    {
        var scheme = useHttps ? "https" : "http";
        var pkgManagerCommand = runtime switch // spaBuilder.Options.PackageManagerCommand
        {
            JsRuntime.Node => "npm",
            JsRuntime.Bun => "bun",
            _ => throw new ArgumentException("Cannot be null or empty", nameof(runtime))
        };

        var sourcePath = spaBuilder.Options.SourcePath;
        var devServerPort = spaBuilder.Options.DevServerPort;
        if (string.IsNullOrEmpty(sourcePath))
        {
            throw new ArgumentException("Cannot be null or empty", nameof(sourcePath));
        }

        if (string.IsNullOrEmpty(scriptName))
        {
            throw new ArgumentException("Cannot be null or empty", nameof(scriptName));
        }

        // Start Vite and attach to middleware pipeline
        var appBuilder = spaBuilder.ApplicationBuilder;
        var applicationStoppingToken = appBuilder.ApplicationServices.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping;
        var logger = LoggerFinder.GetOrCreateLogger(appBuilder, logCategoryName);
        var diagnosticSource = appBuilder.ApplicationServices.GetRequiredService<DiagnosticSource>();
        var port = StartViteServer(sourcePath, scriptName, pkgManagerCommand, devServerPort, logger, diagnosticSource, runtime, applicationStoppingToken);

        var targetUri = new UriBuilder(scheme, "localhost", port).Uri;

        spaBuilder.UseProxyToSpaDevelopmentServer(targetUri);
    }

    private static int StartViteServer(
        string sourcePath,
        string scriptName,
        string pkgManagerCommand,
        int portNumber,
        ILogger logger,
        DiagnosticSource diagnosticSource,
        JsRuntime runtime,
        CancellationToken cancellationToken)
    {
        // When no port is specified, we'll find an available port
        if (portNumber == default)
        {
            portNumber = TcpPortFinder.FindAvailablePort(null);
        }

        // Find an available port for the HMR server
        var hmrPortNumber = TcpPortFinder.FindAvailablePort(portNumber);

        logger.LogInformation("Starting Vite server on port {portNumber}... Hot Module Reload port set to {hmrPortNumber}...", portNumber, hmrPortNumber);

        var envVars = new Dictionary<string, string>
        {
            { "PORT", portNumber.ToString() },
            { "HMR_PORT", hmrPortNumber.ToString() },
            { "BROWSER", "none" }, // We don't want Vite to open its own extra browser window pointing to the internal dev server port
        };

        IScriptRunner? scriptRunner = runtime switch
        {
            JsRuntime.Node => new NodeScriptRunner(sourcePath, scriptName, null, envVars, pkgManagerCommand, diagnosticSource, cancellationToken),
            JsRuntime.Bun => new BunScriptRunner(sourcePath, scriptName, null, envVars, pkgManagerCommand, diagnosticSource, cancellationToken),
            _ => null
        };

        scriptRunner?.AttachToLogger(logger);

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
    /// <param name="runtime">The JS Runtime used for running the script.</param>
    /// <param name="useHttps">Determines if https is be used instead of http for the proxy request.</param>
    public static void UseViteDevelopmentServer(
        this ISpaBuilder spaBuilder,
        string npmScript,
        JsRuntime runtime = JsRuntime.Node,
        bool useHttps = false)
    {
        ArgumentNullException.ThrowIfNull(spaBuilder, nameof(spaBuilder));

        var spaOptions = spaBuilder.Options;

        if (string.IsNullOrEmpty(spaOptions.SourcePath))
            throw new InvalidOperationException(
                $"To use {nameof(UseViteDevelopmentServer)}, you must supply a non-empty value for the {nameof(SpaOptions.SourcePath)} property of {nameof(SpaOptions)} when calling {nameof(SpaApplicationBuilderExtensions.UseSpa)}.");

        ViteDevelopmentServerMiddleware.Attach(spaBuilder, npmScript, runtime, useHttps);
    }
}