# AspNetCore.SpaServices.ViteDevelopmentServer
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
![Nuget](https://img.shields.io/nuget/v/AspNetCore.SpaServices.ViteDevelopmentServer)

With this library you will be able to use [Vite.js](https://vitejs.dev) with your SPA in ASP.NET Core.

Vite.js is an opinionated build tool that enables lightning-fast development by relying on non-bundled
JavaScript modules.

## Install

`$ dotnet add package AspNetCore.SpaServices.ViteDevelopmentServer`

## Usage 
```c#
app.UseSpa(spa =>
{
    spa.Options.SourcePath = "ClientApp";

    if (app.Environment.IsDevelopment()) spa.UseViteDevelopmentServer(npmScript: "dev");
});
```
and if you want to use HMR WebSocket (Hot Reload) you should add
```typescript
 server: {
  port: Number(process.env.PORT),
  hmr: {
      protocol: 'ws',
      host: 'localhost'
    }
}
```
to your vite.config.js or vite.config.ts file

## License

Available under MIT License