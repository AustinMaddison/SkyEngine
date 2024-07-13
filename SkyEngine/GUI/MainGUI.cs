// using ImGuiNET;
//
// namespace SkyEngine;
//
// public class MainGUI
// {
//     private System.Numerics.Vector3 uCloudScale;
//     
//     private int openAction = 1;
//     
//     private void OnDrawGui()
//     {
//         bool noTitlebar = true;
//         bool noScrollbar = false;
//         bool noMenu = true;
//         bool noMove = false;
//         bool noResize = false;
//         bool noCollapse = false;
//         bool noClose = false;
//         bool noNav = false;
//         bool noBackground = false;
//         bool noBringToFront = false;
//         bool unsavedDocument = false;
//
//         bool pOpen = true;
//
//         ImGuiWindowFlags windowFlags = ImGuiWindowFlags.None;
//         if (noTitlebar) windowFlags |= ImGuiWindowFlags.NoTitleBar;
//         if (noScrollbar) windowFlags |= ImGuiWindowFlags.NoScrollbar;
//         if (!noMenu) windowFlags |= ImGuiWindowFlags.MenuBar;
//         if (noMove) windowFlags |= ImGuiWindowFlags.NoMove;
//         if (noResize) windowFlags |= ImGuiWindowFlags.NoResize;
//         if (noCollapse) windowFlags |= ImGuiWindowFlags.NoCollapse;
//         if (noNav) windowFlags |= ImGuiWindowFlags.NoNav;
//         if (noBackground) windowFlags |= ImGuiWindowFlags.NoBackground;
//         if (noBringToFront) windowFlags |= ImGuiWindowFlags.NoBringToFrontOnFocus;
//         if (unsavedDocument) windowFlags |= ImGuiWindowFlags.UnsavedDocument;
//         if (noClose) pOpen = true;
//
//         ImGuiViewportPtr viewport = ImGui.GetMainViewport();
//         ImGui.SetNextWindowPos(new System.Numerics.Vector2(viewport.WorkPos.X+650, viewport.WorkPos.Y+20), ImGuiCond.FirstUseEver);
//         ImGui.SetNextWindowSize(new System.Numerics.Vector2(550, 650), ImGuiCond.FirstUseEver);
//         ImGui.PushItemWidth(ImGui.GetFontSize() * -12);
//
//         ImGui.DockSpaceOverViewport(0, viewport, ImGuiDockNodeFlags.PassthruCentralNode);
//
//         ImGui.SetNextWindowBgAlpha(0.35f); // Transparent background
//
//         if (!ImGui.Begin("Rendering Settings", ref pOpen, windowFlags))
//         {
//             ImGui.End();
//             return;
//         }
//        
//         ImGui.Text("Sky Engine");
//         ImGui.SameLine();
//         ImGui.TextColored(new System.Numerics.Vector4(1.0f, 1.0f, 1.0f, 0.5f), "v1.0");
//         
//         ImGui.Spacing();
//         ImGui.Separator();
//        
//         // Performance Monitor
//         _performanceMonitor.Update();
//         
//         double ylim;
//         ylim = _window.UpdateFrequency * 1.1;
//         ImGui.PlotLines($"FPS {_performanceMonitor.FPS:F}", ref _performanceMonitor.FPSBuffer[0], _performanceMonitor.FPSBuffer.Length, 0, "", 0.0f, (float)ylim, new System.Numerics.Vector2(0.0f, 80.0f));
//         ylim = 1 / _window.UpdateFrequency * 1.1; 
//         ImGui.PlotLines($"Frame Time {_performanceMonitor.FrameTime:R}", ref _performanceMonitor.FrameTimeBuffer[0], _performanceMonitor.FrameTimeBuffer.Length, 0, "", 0.0f,(float)ylim, new System.Numerics.Vector2(0.0f, 80.0f));
//         
//         ImGui.Spacing();
//         ImGui.Separator();
//         ImGui.Spacing();
//
//         if (ImGui.Button("Expand all"))
//             openAction = 1;
//         ImGui.SameLine();
//         if (ImGui.Button("Collapse all"))
//             openAction = 0;
//
//         ImGui.Spacing();
//
//         if (openAction != -1)
//             ImGui.SetNextItemOpen(openAction != 0);
//         if (ImGui.CollapsingHeader("Clouds"))
//         {
//             if (openAction != -1)
//                 ImGui.SetNextItemOpen(openAction != 0);
//             if (ImGui.TreeNode("Shape"))
//             {
//                 if (ImGui.DragFloat3("Cloud Scale", ref uCloudScale, -1f, 1f))
//                 {
//                 }
//                 ImGui.TreePop();
//             }
//
//
//             if (openAction != -1)
//                 ImGui.SetNextItemOpen(openAction != 0);
//             if (ImGui.TreeNode("Wind"))
//             {
//                 if (ImGui.DragFloat3("Cloud Scale", ref uCloudScale, -1f, 1f))
//                 {
//                 }
//
//                 ImGui.TreePop();
//             }
//
//             if (openAction != -1)
//                 ImGui.SetNextItemOpen(openAction != 0);
//             if (ImGui.TreeNode("Shading"))
//             {
//                 if (ImGui.DragFloat3("Cloud Scale", ref uCloudScale, .0f, 1f))
//                 {
//                 }
//
//                 if (ImGui.DragFloat3("Cloud Scale", ref uCloudScale, .0f, 1f))
//                 {
//                 }
//
//                 ImGui.TreePop();
//             }
//         }
//
//         if (openAction != -1)
//             ImGui.SetNextItemOpen(openAction != 0);
//         if (ImGui.CollapsingHeader("Sky"))
//         {
//             if (openAction != -1)
//                 ImGui.SetNextItemOpen(openAction != 0);
//             if (ImGui.TreeNode("Scattering"))
//             {
//                 // if (ImGui.DragFloat3("Cloud Scale", ref uCloudScale, .0f, 1f))
//                 // {
//                 // }
//
//                 ImGui.TreePop();
//             }
//
//             if (openAction != -1)
//                 ImGui.SetNextItemOpen(openAction != 0);
//             if (ImGui.TreeNode("Sun"))
//             {
//                 float[] zenith = new float[360];
//                 for (int i = 0; i < 360; i++)
//                 {
//                     zenith[i] = MathF.Sin(MathF.PI / 180 * i);
//                 }
//                
//                 ImGui.PlotLines("Azimuth", ref zenith[0], 360, 0, "",-1.0f,1.0f, new System.Numerics.Vector2(0.0f, 80.0f));
//                 ImGui.TreePop();
//             }
//         }
//
//         openAction = -1;
//
//         // Menu under title bar
//         if (ImGui.BeginMainMenuBar())
//         {
//             if (ImGui.BeginMenu("Menu"))
//             {
//                 if (ImGui.MenuItem("Quit", "Alt+F4"))
//                 {
//                 }
//
//                 ImGui.EndMenu();
//             }
//
//             ImGui.EndMainMenuBar();
//         }
//         ImGui.ShowDemoWindow();
//
//         ImGuiController.CheckGLError("End of frame");
//     }
// }