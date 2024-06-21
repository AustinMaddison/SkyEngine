using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace SkyEngine;

class App
{
    private const string WindowName = "Sky Engine";
    private const int Width = 640;
    private const int Height = 360;

    static void Main(string[] args)
    {
        Console.WriteLine("Hello World!");

        using (Game game = new Game(Width, Height, WindowName))
        {
            game.Run();
        }
    }
}