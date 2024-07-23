using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using System.Drawing.Imaging;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Serilog;
using Vector4 = System.Numerics.Vector4;

namespace SkyEngine;

public class Engine
{
    private readonly Window _window;

    private const string VertexShaderSource = @"C:\dev\SkyEngine\SkyEngine\Resources\Shaders\vert\uv_quad.vert";
    private const string FragmentShaderSource = @"C:\dev\SkyEngine\SkyEngine\Resources\Shaders\frag\sky.frag";
    private int _texture;


    private readonly List<string> _texturePaths = new List<string>
    {
        @"C:\dev\SkyEngine\SkyEngine\Resources\Textures\noise.png",
        @"C:\dev\SkyEngine\SkyEngine\Resources\Textures\Noise_cloud_dense.png",
        @"C:\dev\SkyEngine\SkyEngine\Resources\Textures\Noise_cloud_less_dense.png",
        @"C:\dev\SkyEngine\SkyEngine\Resources\Textures\Noise_cloud_sparse.png",
        @"C:\dev\SkyEngine\SkyEngine\Resources\Textures\Noise_cloud_sparse_alt.png",
    };

    private int _selectedTextureIndex = 0;
    private int _textureUniformLocation;
    
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
    
    // Shader Uniforms
    private int uCameraMode = 1;
    private System.Numerics.Vector4 uCloudFbmScales;
    private System.Numerics.Vector4 uCloudFbmWeights;
    private System.Numerics.Vector3 uCloudScale;
    private float uCloudSpeed;
    private float uCloudHeight;
    private float uCloudThickness;
    private float uCloudDensity;
    private float uFogDensity;
    private System.Numerics.Vector3 uRayleighCoeff;
    private System.Numerics.Vector3 uMieCoeff;
    private float uSunBrightness;
    private float uEarthRadius ; 
    
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
        
        SetUniformDefaultValues();
        _shader = new Shader(VertexShaderSource, FragmentShaderSource);
        _shader.Use();

        var vertexLocation = _shader.GetAttribLocation("aPosition");
        GL.VertexAttribPointer(vertexLocation, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
        GL.EnableVertexAttribArray(vertexLocation);

        var texCoordLocation = _shader.GetAttribLocation("aTexCoord");
        GL.VertexAttribPointer(texCoordLocation, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
        GL.EnableVertexAttribArray(texCoordLocation);

        _texture = LoadTexture(@"C:\dev\SkyEngine\SkyEngine\Resources\Textures\noise.png");

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

        SetUniforms();
        _shader.Use();

        GL.ClearColor(new Color4(0, 32, 48, 255));
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

        GL.DrawElements(PrimitiveType.Triangles, _triangles.Length, DrawElementsType.UnsignedInt, 0);
    }
    
    private void SetUniforms()
    {
        // Set Noise Cloud Texture
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, _texture);
        GL.Uniform1(GL.GetUniformLocation(_texture, "uNoiseSamp2D"), 0); 

        
        _shader.SetVector2("uResolution", new Vector2(_window.Size.X, _window.Size.Y));
        _shader.SetFloat("uTime", (float)_stopwatch.Elapsed.TotalSeconds);
                
        if (!IsMouseClickingImGuiMenu())
        {
            if (_window.MouseState.IsButtonDown(MouseButton.Left))
            {
                _shader.SetVector2("uMousePos", new Vector2(_window.MouseState.X / _window.Size.X, (_window.Size.Y-_window.MouseState.Y) / _window.Size.Y));   
            }
        }
   
        _shader.SetInt("uCameraMode", uCameraMode);
        
        _shader.SetVector4("uCloudFbmScales", new OpenTK.Mathematics.Vector4(uCloudFbmScales.X, uCloudFbmScales.Y, uCloudFbmScales.Z, uCloudFbmScales.W));
        _shader.SetVector4("uCloudFbmWeights", new OpenTK.Mathematics.Vector4(uCloudFbmWeights.X, uCloudFbmWeights.Y, uCloudFbmWeights.Z, uCloudFbmScales.W));
        
        _shader.SetVector3("uCloudScale", new Vector3(uCloudScale.X, uCloudScale.Y, uCloudScale.Z));
        _shader.SetFloat("uCloudSpeed", uCloudSpeed);
        _shader.SetFloat("uCloudHeight", uCloudHeight);
        _shader.SetFloat("uCloudThickness", uCloudThickness);
        _shader.SetFloat("uCloudDensity", uCloudDensity);
        
        _shader.SetFloat("uFogDensity", uFogDensity * 1/1000);
        _shader.SetVector3("uRayleighCoeff", new Vector3(uRayleighCoeff.X, uRayleighCoeff.Y, uRayleighCoeff.Z));
        _shader.SetVector3("uMieCoeff", new Vector3(uMieCoeff.X * 1/10000, uMieCoeff.Y * 1/10000, uMieCoeff.Z * 1/10000));
        
        _shader.SetFloat("uSunBrightness", uSunBrightness);
        _shader.SetFloat("uEarthRadius", uEarthRadius);
    }

    private void SetUniformDefaultValues()
    {
        uCloudFbmScales = new Vector4(1, 2.0f, 7.0f, 15.0f);
        uCloudFbmWeights = new Vector4(0.6f, 0.25f, 0.125f, 0.0825f);
        uCloudScale = new System.Numerics.Vector3(1, 1, 1);
        uCloudSpeed = 0.02f;
        uCloudHeight = 1600.0f;
        uCloudThickness = 500.0f;
        uCloudDensity = 0.03f;
        uFogDensity = 0.03f;
        
        uRayleighCoeff = new System.Numerics.Vector3(.27f, 0.5f, 1.0f);
        uMieCoeff = new System.Numerics.Vector3(0.5e-6f * 10000);
        
        uSunBrightness = 3.0f;
        uEarthRadius = 6371000.0f; 
    }

   

    private void PushFont(FontStyle fontStyle)
    {
        ImGui.PushFont(_window.GetFont(fontStyle));
    }
    
    
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
            ImGui.Text("Mini Sky Engine");
            ImGui.PopFont(); 
            // ImGui.PopFont(); 
            ImGui.SameLine();
            
            PushFont(FontStyle.REG);
            ImGui.TextColored(new System.Numerics.Vector4(1.0f, 1.0f, 1.0f, 0.5f), "v1.0");
            ImGui.PopFont(); 
        }
        CreatePerformanceMenu();

        ImGui.Spacing();
        ImGui.Text("Settings");
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        CreateUniformControls();

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

        ImGuiController.CheckGLError("End of frame");
    }
    
    private bool IsMouseClickingImGuiMenu()
    {
        return (ImGui.IsAnyItemActive() | ImGui.IsAnyItemFocused() | ImGui.IsAnyItemHovered() && ImGui.IsMouseDown(ImGuiMouseButton.Left));
    }

    private void CreatePerformanceMenu()
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
        ImGui.Text($"Window Size x:{_window.Size.X} y:{_window.Size.Y}");
        ImGui.Text($"Window Position x:{_window.ClientLocation.X} y:{_window.ClientLocation.Y}");
    }
    private void CreateUniformControls()
    {
        if (ImGui.Button("Reset"))
            SetUniformDefaultValues();
        
        if (ImGui.CollapsingHeader("Uniforms"))
        {
            if (ImGui.TreeNode("Clouds"))
            {
                CreateCloudNoiseTextureDropdown();
                ImGui.DragFloat4("Cloud FBM Scale", ref uCloudFbmScales, 0.01f, 0.0f, 100.0f);
                ImGui.DragFloat4("Cloud FBM Weight", ref uCloudFbmWeights, 0.01f, 0.0f, 100.0f);

                ImGui.DragFloat3("Cloud Scale", ref uCloudScale, 0.01f, 0.0f, 10.0f);
                ImGui.DragFloat("Cloud Speed", ref uCloudSpeed, 0.001f, 0.0f, 1.0f);
                ImGui.DragFloat("Cloud Height", ref uCloudHeight, 1.0f, 0.0f, 10000.0f);
                ImGui.DragFloat("Cloud Thickness", ref uCloudThickness, 1.0f, 0.0f, 10000.0f);
                ImGui.DragFloat("Cloud Density", ref uCloudDensity, 0.0001f, 0.0f, 1.0f);
                ImGui.TreePop();
            }

            if (ImGui.TreeNode("Fog"))
            {
                ImGui.DragFloat("Fog Density", ref uFogDensity, 0.001f, 0.0f, 1f);
                ImGui.TreePop();
            }

            if (ImGui.TreeNode("Scattering"))
            {
                ImGui.DragFloat3("Rayleigh Coeff", ref uRayleighCoeff, 0.01f, 0.0f, 10.0f);
                ImGui.DragFloat3("Mie Coeff", ref uMieCoeff, 0.01f, 0.0f, 1f);
                ImGui.TreePop();
            }

            if (ImGui.TreeNode("Sun"))
            {
                ImGui.DragFloat("Sun Brightness", ref uSunBrightness, 0.1f, 0.0f, 100.0f);
                ImGui.TreePop();
            }

            if (ImGui.TreeNode("Earth"))
            {
                ImGui.DragFloat("Earth Radius", ref uEarthRadius, 1.0f, 0.0f, 10000000.0f);
                ImGui.TreePop();
            }

            if (ImGui.TreeNode("Camera"))
            {
                bool tmp = uCameraMode == 1;
                ImGui.Checkbox("Camera Mode", ref tmp);
                uCameraMode = tmp ? 1 : 0;
                
                ImGui.TreePop();
            }
        }
    }
    

    private void OnUpdate()
    {
    }

    public void Dispose()
    {
        _window.Dispose();
    }
    
    private void CreateCloudNoiseTextureDropdown()
    {
        if (ImGui.BeginCombo("Cloud Noise Texture", _texturePaths[_selectedTextureIndex]))
        {
            for (int i = 0; i < _texturePaths.Count; i++)
            {
                bool isSelected = (_selectedTextureIndex == i);
                if (ImGui.Selectable(_texturePaths[i], isSelected))
                {
                    _selectedTextureIndex = i;
                    LoadAndSetTexture(_texturePaths[_selectedTextureIndex]);
                }
                if (isSelected)
                {
                    ImGui.SetItemDefaultFocus();
                }
            }
            ImGui.EndCombo();
        }
    }
    
    public int LoadTexture(string path)
    {
        Bitmap bitmap = new Bitmap(path);
        int texture;

        GL.GenTextures(1, out texture);
        GL.BindTexture(TextureTarget.Texture2D, texture);

        BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);

        bitmap.UnlockBits(data);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

        GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

        return texture;
    }
    
    private void LoadAndSetTexture(string texturePath)
    {
        _texture = LoadTexture(texturePath);
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, _texture);
        GL.Uniform1(_textureUniformLocation, 0);
    }
}