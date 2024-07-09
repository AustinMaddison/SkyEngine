using System.Diagnostics;
using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Serilog;

namespace SkyEngine;

public class Engine
{
    private readonly Window _window;

    private const string VertexShaderSource = "C:\\dev\\SkyEngine\\SkyEngine\\Shader\\vert.glsl";
    private const string FragmentShaderSource = "C:\\dev\\SkyEngine\\SkyEngine\\Shader\\vornoi.glsl";

    private readonly float[] _vertices =
    [
        -1.0f, -1.0f, 0.0f, 0.0f, 0.0f, // lb
        1.0f, -1.0f, 0.0f, 1.0f, 0.0f,  // rb
        -1.0f, 1.0f, 0.0f, 0.0f, 1.0f,  // lt
        1.0f, 1.0f, 0.0f, 1.0f, 1.0f    // rt
    ];

    private readonly int[] _triangles =
    [
        0, 2, 1, // bot left
        1, 2, 3  // top right
    ];

    private int _elementBufferObject;
    private int _vertexBufferObject;
    private int _vertexArrayObject;
    private Shader _shader;

    private Stopwatch _stopwatch;

    private static Engine _instance = null;

    private Engine(int width, int height, string title)
    {
        _window = new Window(width, height, title);
        _window.bindUpdateCallback(OnUpdate);
        _window.bindRenderCallback(OnRender);
        _window.bindDrawGUICallback(OnDrawGui);
        OnLoad();
    }

    public static Engine Create(int width, int height, string title)
    {
        if (_instance != null)
        {
            Log.Error("This singleton has already been created.");
            return _instance;
        }

        _instance = new Engine(width, height, title);
        return _instance;
    }

    public static Engine Instance
    {
        get
        {
            if (_instance == null)
            {
                Log.Error("This singleton has not been created.");
            }

            return _instance;
        }
    }

    public void Run()
    {
        _window.Run();
    }

    private void OnLoad()
    {
        _window.Clear();

        _vertexArrayObject = GL.GenVertexArray();
        GL.BindVertexArray(_vertexArrayObject);

        _vertexBufferObject = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
        GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Length * sizeof(float), _vertices, BufferUsageHint.StaticDraw);

        _elementBufferObject = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _elementBufferObject);
        GL.BufferData(BufferTarget.ElementArrayBuffer, _triangles.Length * sizeof(uint), _triangles, BufferUsageHint.StaticDraw);

        _shader = new Shader(VertexShaderSource, FragmentShaderSource);
        _shader.Use();

        var vertexLocation = _shader.GetAttribLocation("aPosition");
        GL.VertexAttribPointer(vertexLocation, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
        GL.EnableVertexAttribArray(vertexLocation);

        var texCoordLocation = _shader.GetAttribLocation("aTexCoord");
        GL.VertexAttribPointer(texCoordLocation, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
        GL.EnableVertexAttribArray(texCoordLocation);

        Log.Information("OpenGL {Version}", GL.GetString((StringName.Version)));
        Log.Information("OpenGL {Extensions}", GL.GetString((StringName.Extensions)));
        _stopwatch = new Stopwatch();
        _stopwatch.Start();
    }

    public void RecompileShader()
    {
        Log.Information("Recompiling shader.");
        Thread.Sleep(1000);
        _shader = new Shader(VertexShaderSource, FragmentShaderSource);
    }

    private void OnRender()
    {
        _window.Clear();
        GL.BindVertexArray(_vertexArrayObject);

        _shader.SetVector2("uResolution", new Vector2(_window.Size.X, _window.Size.Y));
        _shader.SetFloat("uTime", (float)_stopwatch.Elapsed.TotalSeconds);
        // _shader.SetVector2("uMousePos", new Vector2(MouseState.X / _window.Size.X, (_window.Size.Y-Window.MouseState.Y) / _window.Size.Y));
        _shader.SetVector2("uMousePos", new Vector2(1f, 1f));
        _shader.SetVector3("uCloudScale", new Vector3(uCloudScale.X, uCloudScale.Y, uCloudScale.Z));
        _shader.SetInt("uMouseBtnDown", 1);
        // _shader.SetInt("uMouseBtnDown", _window.MouseState.IsButtonDown(MouseButton.Left) ? 1 : 0);
        _shader.Use();

        GL.ClearColor(new Color4(0, 32, 48, 255));
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

        GL.DrawElements(PrimitiveType.Triangles, _triangles.Length, DrawElementsType.UnsignedInt, 0);
    }


    private System.Numerics.Vector3 uCloudScale;
    private void OnDrawGui()
    {
        bool noTitlebar = false;
        bool noScrollbar = false;
        bool noMenu = false;
        bool noMove = false;
        bool noResize = false;
        bool noCollapse = false;
        bool noClose = false;
        bool noNav = false;
        bool noBackground = false;
        bool noBringToFront = false;
        bool unsavedDocument = false;

        bool pOpen = true;

        ImGuiWindowFlags windowFlags = ImGuiWindowFlags.None;
        if (noTitlebar) windowFlags |= ImGuiWindowFlags.NoTitleBar;
        if (noScrollbar) windowFlags |= ImGuiWindowFlags.NoScrollbar;
        if (!noMenu) windowFlags |= ImGuiWindowFlags.MenuBar;
        if (noMove) windowFlags |= ImGuiWindowFlags.NoMove;
        if (noResize) windowFlags |= ImGuiWindowFlags.NoResize;
        if (noCollapse) windowFlags |= ImGuiWindowFlags.NoCollapse;
        if (noNav) windowFlags |= ImGuiWindowFlags.NoNav;
        if (noBackground) windowFlags |= ImGuiWindowFlags.NoBackground;
        if (noBringToFront) windowFlags |= ImGuiWindowFlags.NoBringToFrontOnFocus;
        if (unsavedDocument) windowFlags |= ImGuiWindowFlags.UnsavedDocument;
        if (noClose) pOpen = true;


        ImGuiViewportPtr viewport = ImGui.GetMainViewport();
        ImGui.SetNextWindowPos(new System.Numerics.Vector2(viewport.WorkPos.X+650, viewport.WorkPos.Y+20), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(550, 650), ImGuiCond.FirstUseEver);
        ImGui.PushItemWidth(ImGui.GetFontSize() * -12);
        ImGui.DockSpaceOverViewport(0, viewport, ImGuiDockNodeFlags.PassthruCentralNode);


        if (!ImGui.Begin("Rendering Settings", ref pOpen, windowFlags))
        {
            ImGui.End();
            return;
        }

        if (ImGui.SliderFloat3("Cloud Scale", ref uCloudScale, .0f, 1f))
        {
        }
        

        // ImGui.CheckboxFlags(ImGui.Pa);
        // ImGui.ShowDemoWindow();
        // bool my_tool_active = true;
        // if (ImGui.BeginMenu("Options"))
        // {
        //     if(ImGui.MenuItem("JFFF", ""))
        //     {
        //         ImGui.SliderFloat3("s", ref uCloudScale, .0f, 1.0f);
        //     }
        //     if (ImGui.BeginMenuBar())
        //     {
        //     }
        //     
        // if (ImGui.SliderFloat3("s", ref uCloudScale, .0f, 1.0f))
        // {
        //     
        // }


        // }
        // if (ImGui.BeginMenuBar())
        // {
        //   if (ImGui.SliderFloat3("Cloud Noise Scale", ref uCloudScale, .0f, .0f))
        //     {
        //     
        //     }
        //     if (ImGui.BeginMenu("Menu"))
        //     {
        //         if (ImGui.MenuItem("Close", "Q"))
        //         {
        //           
        //         }
        //
        //         ImGui.EndMenu();
        //     }
        //
        //     ImGui.EndMenuBar();
        // }
        //
        // if (ImGui.BeginMainMenuBar())
        // {
        //     if (ImGui.BeginMenu("File"))
        //     {
        //         if (ImGui.MenuItem("Open", "CTRL+O"))
        //         {
        //         }
        //
        //         ImGui.Separator();
        //         if (ImGui.MenuItem("Save", "CTRL+S"))
        //         {
        //             
        //         }
        //
        //         if (ImGui.MenuItem("Save As", "CTRL+Shift+S"))
        //         {
        //         }
        //
        //         ImGui.EndMenu();
        //     }
        //
        //     if (ImGui.BeginMenu("Settings"))
        //     {
        //         bool enabled = true;
        //         ImGui.Checkbox("Enabled", ref enabled);
        //         ImGui.EndMenu();
        //     }
        //
        //     ImGui.EndMainMenuBar();
        // }

        ImGuiController.CheckGLError("End of frame");
    }

    private void OnUpdate()
    {
    }

    public void Dispose()
    {
        _window.Dispose();
    }
}