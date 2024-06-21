using OpenTK.Graphics.OpenGL4;

namespace SkyEngine;

public class Shader
{
   private int Handle;

   public Shader(string vertexShaderPath, string fragmentShaderPath)
   {
      string vertexShaderSource = File.ReadAllText(vertexShaderPath);
      string fragmentShaderSource = File.ReadAllText(fragmentShaderPath);

      int vertexShader = GL.CreateShader(ShaderType.VertexShader);
      GL.ShaderSource(vertexShader, vertexShaderSource);
      
      int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
      GL.ShaderSource(fragmentShader, fragmentShaderSource);
     
      // Compile Shaders
      GL.CompileShader(vertexShader);
      GL.GetShader(vertexShader, ShaderParameter.CompileStatus, out int success);
      if (success == 0)
      {
         string infoLog = GL.GetShaderInfoLog(vertexShader);
         Console.WriteLine(infoLog);
      }
      
      GL.CompileShader(fragmentShader);
      GL.GetShader(fragmentShader, ShaderParameter.CompileStatus, out success);
      if (success == 0)
      {
         string infoLog = GL.GetShaderInfoLog(fragmentShader);
         Console.WriteLine(infoLog);
      }

      Handle = GL.CreateProgram();
      
      GL.AttachShader(Handle, vertexShader);
      GL.AttachShader(Handle, fragmentShader);
      
      GL.LinkProgram(Handle);
      
      GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out success);
      if (success == 0)
      {
         string infoLog = GL.GetProgramInfoLog(Handle);
         Console.WriteLine(infoLog);
      }
   }

   public void Use()
   {
      GL.UseProgram(Handle);
   }
   
   private bool _disposedValue = false;

   protected virtual void Dispose(bool disposing)
   {
      if (!_disposedValue)
      {
         GL.DeleteProgram(Handle);

         _disposedValue = true;
      }
   }

   ~Shader()
   {
      if (_disposedValue == false)
      {
         Console.WriteLine("GPU Resource leak! Did you forget to call Dispose()?");
      }
   }
   
   public void Dispose()
   {
      Dispose(true);
      GC.SuppressFinalize(this);
   }

}