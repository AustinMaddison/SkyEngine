using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.Runtime.InteropServices;
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

    private const string VertexShaderSource = @"C:\dev\SkyEngine\SkyEngine\Resources\Shaders\vert\uv_quad.vert";
    private const string FragmentShaderSource = @"C:\dev\SkyEngine\SkyEngine\Resources\Shaders\frag\vornoi.frag";

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
    private PerformanceMonitor _performanceMonitor;

    private ImFontPtr fontReg;
    private  ImFontPtr fontBg;
    
    private static Engine _instance = null;

    private Engine(int width, int height, int updateFrequency, string title)
    {
        _window = new Window(width, height, title);
        
        OnLoad();
        _window.UpdateFrequency = updateFrequency;
        _window.bindUpdateCallback(OnUpdate);
        _window.bindRenderCallback(OnRender);
        _window.bindDrawGUICallback(OnDrawGui);
    }

    public static Engine Create(int width, int height, int updateFrequency, string title)
    {
        if (_instance != null)
        {
            Log.Error("This singleton has already been created.");
            return _instance;
        }

        _instance = new Engine(width, height, updateFrequency, title);
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

        Log.Information("OpenGL {Version}", GL.GetString(StringName.Version));
        Log.Information("OpenGL {Extensions}", GL.GetString(StringName.Extensions));
        
        _stopwatch = new Stopwatch();
        _performanceMonitor = new PerformanceMonitor();
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
        _shader.SetVector2("uMousePos", new Vector2(_window.MouseState.X / _window.Size.X, (_window.Size.Y-_window.MouseState.Y) / _window.Size.Y));
        // _shader.SetVector2("uMousePos", new Vector2(1f, 1f));
        _shader.SetVector3("uCloudScale", new Vector3(uCloudScale.X, uCloudScale.Y, uCloudScale.Z));
        _shader.SetInt("uMouseBtnDown", 1);
        // _shader.SetInt("uMouseBtnDown", _window.MouseState.IsButtonDown(MouseButton.Left) ? 1 : 0);
        _shader.Use();

        GL.ClearColor(new Color4(0, 32, 48, 255));
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

        GL.DrawElements(PrimitiveType.Triangles, _triangles.Length, DrawElementsType.UnsignedInt, 0);
    }

    private unsafe void CreateFont(string fontFile, float fontSize, byte mergeMode, ushort[] charRange)
    {
        // create the object on the native side
        var nativeConfig = ImGuiNative.ImFontConfig_ImFontConfig();

        // fill with data
        (*nativeConfig).OversampleH = 3;
        (*nativeConfig).OversampleV = 3;
        (*nativeConfig).RasterizerMultiply = 1f;
        (*nativeConfig).GlyphExtraSpacing = new System.Numerics.Vector2(0, 0);
        (*nativeConfig).MergeMode = mergeMode;

        GCHandle rangeHandle = GCHandle.Alloc(charRange, GCHandleType.Pinned);
        try
        {
            ImGui.GetIO().Fonts.AddFontFromFileTTF(fontFile, fontSize, nativeConfig, rangeHandle.AddrOfPinnedObject());
        }
        finally
        {
            if (rangeHandle.IsAllocated)
                rangeHandle.Free();
        }

        // delete the reference. ImGui copies it
        ImGuiNative.ImFontConfig_destroy(nativeConfig);
    }

    private void PushFont(FontStyle fontStyle)
    {
        ImGui.PushFont(_window.GetFont(fontStyle));
    }
    
    private System.Numerics.Vector3 uCloudScale;
    private int openAction = 1;
    private void OnDrawGui()
    {

        // ImGui.PushFont(fontReg); 
        
        bool noTitlebar = false;
        bool noScrollbar = false;
        bool noMenu = true;
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

        // ImGui.SetNextWindowBgAlpha(0.85f); // Transparent background

        if (!ImGui.Begin("Rendering Settings", ref pOpen, windowFlags))
        {
            ImGui.End();
            return;
        }
        
        {
            PushFont(FontStyle.SBD_L);
            ImGui.Text("Sky Engine");
            ImGui.PopFont(); 
            // ImGui.PopFont(); 
            ImGui.SameLine();
            
            PushFont(FontStyle.REG);
            ImGui.TextColored(new System.Numerics.Vector4(1.0f, 1.0f, 1.0f, 0.5f), "v1.0");
            ImGui.PopFont(); 
        }

        {
            PushFont(FontStyle.MED);
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            ImGui.Text("Stats");
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            // Performance Monitor
            _performanceMonitor.Update();

            double ylim;
            ylim = _window.UpdateFrequency * 1.1;
            ImGui.PlotLines($"FPS {_performanceMonitor.FPS:F}",
                ref _performanceMonitor.FPSBuffer[0],
                _performanceMonitor.FPSBuffer.Length,
                0,
                "",
                0.0f,
                (float)ylim,
                new System.Numerics.Vector2(0.0f, 80.0f)
            );
            ylim = 1 / _window.UpdateFrequency * 1.1;
            ImGui.PlotLines($"Frame Time {_performanceMonitor.FrameTime:R}",
                ref _performanceMonitor.FrameTimeBuffer[0],
                _performanceMonitor.FrameTimeBuffer.Length,
                0,
                "",
                0.0f,
                (float)ylim,
                new System.Numerics.Vector2(0.0f, 80.0f)
            );

            ImGui.Text($"Mouse x:{_window.MouseState.X / _window.Size.X} y:{(_window.Size.Y-_window.MouseState.Y) / _window.Size.Y}");
        }
        
        ImGui.Spacing();
        ImGui.Text("Settings");
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.Button("Expand all"))
            openAction = 1;
        ImGui.SameLine();
        if (ImGui.Button("Collapse all"))
            openAction = 0;

        ImGui.Spacing();

        if (openAction != -1)
            ImGui.SetNextItemOpen(openAction != 0);
        if (ImGui.CollapsingHeader("Clouds"))
        {
            if (openAction != -1)
                ImGui.SetNextItemOpen(openAction != 0);
            if (ImGui.TreeNode("Shape"))
            {
                if (ImGui.DragFloat3("Cloud Scale", ref uCloudScale, -1f, 1f))
                {
                }

                ImGui.TreePop();
            }


            if (openAction != -1)
                ImGui.SetNextItemOpen(openAction != 0);
            if (ImGui.TreeNode("Wind"))
            {
                if (ImGui.DragFloat3("Cloud Scale", ref uCloudScale, -1f, 1f))
                {
                }

                ImGui.TreePop();
            }

            if (openAction != -1)
                ImGui.SetNextItemOpen(openAction != 0);
            if (ImGui.TreeNode("Shading"))
            {
                if (ImGui.DragFloat3("Cloud Scale", ref uCloudScale, .0f, 1f))
                {
                }

                if (ImGui.DragFloat3("Cloud Scale", ref uCloudScale, .0f, 1f))
                {
                }

                ImGui.TreePop();
            }
        }

        if (openAction != -1)
            ImGui.SetNextItemOpen(openAction != 0);
        if (ImGui.CollapsingHeader("Sky"))
        {
            if (openAction != -1)
                ImGui.SetNextItemOpen(openAction != 0);
            if (ImGui.TreeNode("Scattering"))
            {
                // if (ImGui.DragFloat3("Cloud Scale", ref uCloudScale, .0f, 1f))
                // {
                // }

                ImGui.TreePop();
            }

            if (openAction != -1)
                ImGui.SetNextItemOpen(openAction != 0);
            if (ImGui.TreeNode("Sun"))
            {
                float[] zenith = new float[360];
                for (int i = 0; i < 360; i++)
                {
                    zenith[i] = MathF.Sin(MathF.PI / 180 * i);
                }
               
                ImGui.PlotLines("Azimuth", ref zenith[0], 360, 0, "",-1.0f,1.0f, new System.Numerics.Vector2(0.0f, 80.0f));
                ImGui.TreePop();
            }
        }

        openAction = -1;

        // Menu under title bar
        if (ImGui.BeginMainMenuBar())
        {
            if (ImGui.BeginMenu("Menu"))
            {
                if (ImGui.MenuItem("Quit", "Alt+F4"))
                {
                }

                ImGui.EndMenu();
            }

            ImGui.EndMainMenuBar();
        }
        // ImGui.ShowDemoWindow();

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