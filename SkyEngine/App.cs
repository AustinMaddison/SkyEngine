using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace SkyEngine;
class App
{
    private const string WindowName = "Sky Engine";
    private const int Width = 512;
    private const int Height = 512;

    static void Main(string[] args)
    {
        Console.WriteLine(WindowName+" V0.01 Launching...");

        using (Renderer renderer = new Renderer(Width, Height, WindowName))
        {
            renderer.Run();
        }
    }
}