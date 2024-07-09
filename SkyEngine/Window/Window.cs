using System.Diagnostics;
using ImGuiNET;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace SkyEngine
{
    public delegate void onEventCallback(); 
    
    public class Window : GameWindow
    {
        private ImGuiController _controller;
        private Input _input;

        private onEventCallback OnUpdate;
        private onEventCallback OnRender;
        private onEventCallback OnDrawGUI;
        
        private readonly Color4 _clearColor = new Color4(.0f, .0f, .0f, 1f);

        public Window(int width, int height, string title) :
            base(GameWindowSettings.Default,
                new NativeWindowSettings
                {
                    ClientSize = (width, height),
                    Title = title,
                    Vsync = VSyncMode.On,
                    Flags = ContextFlags.ForwardCompatible
                }
            )
        {
        }
        public void bindUpdateCallback(onEventCallback call)
        {
            OnUpdate = call;
        }
        
        public void bindRenderCallback(onEventCallback call)
        {
            OnRender = call;
        }
        
        public void bindDrawGUICallback(onEventCallback call)
        {
            OnDrawGUI = call;
        }
        
        protected override void OnLoad()
        {
            Title += ": OpenGL Version: "+GL.GetString(StringName.Version);
            
            _controller = new ImGuiController(ClientSize.X, ClientSize.Y);
            _input = new Input(this);
            
            GL.ClearColor(_clearColor);
            base.OnLoad();
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            _controller.Update(this, (float)e.Time);
            
            Clear();
            OnRender?.Invoke();
            
            OnDrawGUI?.Invoke();
            _controller.Render();
            
            ImGuiController.CheckGLError("End of frame");
            SwapBuffers();
        }

        public void Clear()
        {
            GL.ClearColor(_clearColor);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
        }

        public void Clear(Color4 color)
        {
            GL.ClearColor(color);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
        }
        

        protected override void OnFramebufferResize(FramebufferResizeEventArgs e)
        {
            GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);
            _controller.WindowResized(ClientSize.X, ClientSize.Y);
            
            base.OnFramebufferResize(e);
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);
            _input.OnUpdateFrame();
            OnUpdate?.Invoke();
        }

        protected override void OnTextInput(TextInputEventArgs e)
        {
            base.OnTextInput(e);
            _controller.PressChar((char)e.Unicode);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            _controller.MouseScroll(e.Offset);
        }
        
    }
}