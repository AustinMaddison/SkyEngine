using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Buffer = OpenTK.Graphics.OpenGL4.Buffer;

namespace SkyEngine;

public class Game : GameWindow
{
    private Shader _shader;

    private readonly float[] _vertices =
    {
        -0.5f, -0.5f, 0.0f, // left bot
        0.5f, -0.5f, 0.0f, // right bot
        0.0f,  0.5f, 0.0f // top mid
    };
    
    private int _vertexBufferObject;
    private int _vertexArrayObject;
        
    public Game(int width, int height, string title) :
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

        _vertexBufferObject = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
        GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Length * sizeof(float), _vertices, BufferUsageHint.StaticDraw);

        _vertexArrayObject = GL.GenVertexArray();
        GL.BindVertexArray(_vertexArrayObject);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);

        _shader = new Shader("/home/salti/dev/SkyRenderer/SkyEngine/SkyEngine/Shaders/vert.glsl", "/home/salti/dev/SkyRenderer/SkyEngine/SkyEngine/Shaders/frag.glsl");
        _shader.Use();
    }

    protected override void OnRenderFrame(FrameEventArgs e)
    {
        base.OnRenderFrame(e);
        
        GL.Clear(ClearBufferMask.ColorBufferBit);
        
        _shader.Use();
        
        GL.BindVertexArray(_vertexArrayObject);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
        
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