using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Configuration;
using System.Collections.Specialized;
using Serilog;
using System;
using Serilog.Context;
using Serilog.Core;

namespace SkyEngine;

class Program
{
    static async Task Main()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .CreateLogger();


    }
    static void Main(string[] args)
    {
        Main();
        Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
        GetAppSettings(out int width, out int height, out string title);
        Log.Information("Configuration Loaded: {Width} {Height} {Title}", width, height, title);
        
        Engine engine = new Engine(width, height, title);
        engine.Run();
    }

    private static bool GetAppSettings(out int width, out int height, out string title)
    {
        NameValueCollection appSettings = ConfigurationManager.AppSettings;
        bool success = true;

        string widthSetting = appSettings["width"];
        if (!int.TryParse(widthSetting, out width))
        {
            width = 500;
            success = false;
            Log.Error("Couldn't get width in App.config. Setting width to {default width}", width);
        }

        string heightSetting = appSettings["height"];
        if (!int.TryParse(heightSetting, out height))
        {
            height = 500;
            success = false;
            Log.Error("Couldn't get height in App.config. Setting height to {default height}", height);
        }

        title = appSettings["title"] + " " + appSettings["version"];
        if (title == null)
        {
            title = "Default Title";
            success = false;
            Log.Error("Couldn't get title in App.config. Setting title to {default title}", title);
        }
        return success;
    }
}