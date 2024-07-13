using System.Diagnostics;

namespace SkyEngine;

public class PerformanceMonitor
{
   // private Dictionary<string, object> data;
   
   private Stopwatch _stopwatch;
   private double _frameTime;
   private double _fps;
   private List<float> _fpsBuffer;
   private List<float> _frameTimeBuffer;
   private int _frameCount;
   private double _elapsedTime;
   private double _refreshRate;
   private readonly int _bufferSize;

   public double FPS => _fps;
   public double FrameTime => _frameTime;
   public float[] FPSBuffer => _fpsBuffer.ToArray();
   public float[] FrameTimeBuffer => _frameTimeBuffer.ToArray();

   public PerformanceMonitor(double refreshRate = 1.0/30, int bufferSize = 100)
   {
      _stopwatch = new Stopwatch();
      _frameTime = 0.0;
      _fps = 0.0;
      _frameCount = 0;
      _elapsedTime = 0.0;
      _refreshRate = refreshRate;
      
      _fpsBuffer = new List<float>(new float[bufferSize]);
      _frameTimeBuffer = new List<float>(new float[bufferSize]);
      _bufferSize = bufferSize;
      
      _stopwatch.Start();
   }
   
   public void Update()
   {
      _frameTime = _stopwatch.Elapsed.TotalSeconds;
      _stopwatch.Restart();

      _frameCount++;
      _elapsedTime += _frameTime;

      if (_elapsedTime > _refreshRate)
      {
         _fps = _frameCount / _elapsedTime;
         _frameCount = 0;
         _elapsedTime = 0.0;
         
         UpdateBuffer(_fpsBuffer, (float)_fps);
         UpdateBuffer(_frameTimeBuffer, (float)_frameTime);
      }
   }

   private void UpdateBuffer(List<float> buffer, float value)
   {
      if (buffer.Count >= _bufferSize)
      {
         buffer.RemoveAt(0);
      }
      buffer.Add(value);
   }
   
}