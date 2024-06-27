using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using All = OpenTK.Graphics.ES11.All;
using Buffer = OpenTK.Graphics.OpenGL4.Buffer;

namespace SkyEngine;

public class Renderer : GameWindow
{

    private readonly float[] _vertices =
    [
        -1.0f, -1.0f, 0.0f, 0.0f,  0.0f, // lb
         1.0f, -1.0f, 0.0f, 1.0f,  0.0f, // rb
        -1.0f,  1.0f, 0.0f, 0.0f,  1.0f, // lt
         1.0f,  1.0f, 0.0f, 1.0f,  1.0f  // rt
    ];

    private readonly int[] _triangles =
    [
        0, 2, 1,  // bot left
        1, 2, 3   // top right
    ];
    
    private int _elementBufferObject;
    private int _vertexBufferObject;
    private int _vertexArrayObject;
    private Shader _shader;
    
    private FileSystemWatcher _shaderWatcher;

        
    public Renderer(int width, int height, string title) :
        base(GameWindowSettings.Default, 
            new NativeWindowSettings()
            {
                Size = (width, height),
                Title = title
            }
        )
    {
    }

    protected override void OnLoad()
    {
        base.OnLoad();
        GL.ClearColor(0.3f, 0.3f, 0.3f, 1f);

        _vertexArrayObject = GL.GenVertexArray();
        GL.BindVertexArray(_vertexArrayObject);
        
        _vertexBufferObject = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
        GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Length * sizeof(float), _vertices, BufferUsageHint.StaticDraw);
        
        _elementBufferObject = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _elementBufferObject);
        GL.BufferData(BufferTarget.ElementArrayBuffer, _triangles.Length * sizeof(uint), _triangles, BufferUsageHint.StaticDraw);
        
        _shader = new Shader("/home/salti/dev/SkyRenderer/SkyEngine/SkyEngine/Shaders/vert.glsl", "/home/salti/dev/SkyRenderer/SkyEngine/SkyEngine/Shaders/frag.glsl");
        _shader.Use();

        var vertexLocation = _shader.GetAttribLocation("aPosition");
        GL.VertexAttribPointer(vertexLocation, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
        GL.EnableVertexAttribArray(vertexLocation);

        var texCoordLocation = _shader.GetAttribLocation("aTexCoord");
        GL.VertexAttribPointer(texCoordLocation, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
        GL.EnableVertexAttribArray(texCoordLocation);

        _shaderWatcher = new FileSystemWatcher
        {
            Path = Path.GetDirectoryName("/home/salti/dev/SkyRenderer/SkyEngine/SkyEngine/Shaders/frag.glsl"),
            Filter = Path.GetFileName("/home/salti/dev/SkyRenderer/SkyEngine/SkyEngine/Shaders/frag.glsl"),
            // Filter = "*.glsl",
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
        };
        _shaderWatcher.Changed += OnShaderChanged; 
        _shaderWatcher.EnableRaisingEvents = true;
    }
    
    private void OnShaderChanged(Object sender, FileSystemEventArgs args)
    {
        _shader = new Shader("/home/salti/dev/SkyRenderer/SkyEngine/SkyEngine/Shaders/vert.glsl", "/home/salti/dev/SkyRenderer/SkyEngine/SkyEngine/Shaders/frag.glsl");
        _shader.Use();
        Console.WriteLine("Recompiling Shader");
    }
    
    protected override void OnRenderFrame(FrameEventArgs e)
    {
        base.OnRenderFrame(e);
        
        GL.Clear(ClearBufferMask.ColorBufferBit);
        
        GL.BindVertexArray(_vertexArrayObject);
        
        _shader.Use();
        
        GL.DrawElements(PrimitiveType.Triangles, _triangles.Length, DrawElementsType.UnsignedInt, 0);
        
        SwapBuffers();
    }

    protected override void OnFramebufferResize(FramebufferResizeEventArgs e)
    {
        base.OnFramebufferResize(e);
        GL.Viewport(0, 0, e.Width, e.Height);
    }

    protected override void OnUpdateFrame(FrameEventArgs e)
    {
        base.OnUpdateFrame(e);

        // Exit
        if (KeyboardState.IsKeyDown(Keys.Escape))
        {
            Close();
        }
    }
    
 
}