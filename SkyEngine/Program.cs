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
    static async Task Logger()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .CreateLogger();
        
        Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
    }
    
    static void Main(string[] args)
    {
        Logger();
        if (GetAppSettings(out int width, out int height, out int updateFrequency, out string title))
        {
            Log.Error("Failed getting all app settings parameters.");
        }
        Log.Information("Configuration Loaded: {Title} w:{Width} h:{Height} hz:{UpdateFrequency}", title, width, height, updateFrequency);
        
        Engine.Create(width, height, updateFrequency, title);
        Engine.Instance.Run();
    }
    
    private static bool GetAppSettings(out int width, out int height, out int updateFrequency, out string title)
    {
        NameValueCollection appSettings = ConfigurationManager.AppSettings;
        bool success = true;

        string? widthSetting = appSettings["width"];
        if (!int.TryParse(widthSetting, out width))
        {
            width = 500;
            success = false;
            Log.Error("Couldn't get width in App.config. Setting width to {default width}", width);
        }

        string? heightSetting = appSettings["height"];
        if (!int.TryParse(heightSetting, out height))
        {
            height = 500;
            success = false;
            Log.Error("Couldn't get height in App.config. Setting height to {default height}", height);
        }
        
        string? updateFrequencySettings = appSettings["updateFrequency"];
        if (!int.TryParse(updateFrequencySettings, out updateFrequency))
        {
            updateFrequency = 60;
            success = false;
            Log.Error("Couldn't get update frequency in App.config. Setting frequency to {default updateFrequency}", updateFrequency);
        }
        
        string? titleName = appSettings["title"];
        string? titleVersion = appSettings["version"];
        title = titleName + " " + titleVersion;
        if (titleName == null | titleVersion == null)
        {
            title = "Default Title";
            success = false;
            Log.Error("Couldn't get title in App.config. Setting title to {default title}", title);
        }
        
        return success;
    }
}