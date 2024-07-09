using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Input;
using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;


namespace SkyEngine; 
    ///<summary>nice little wrap around default input system</summary>
    /// <summary>current and previous keyboard input states. Used to find if user pressed of released the key</summary>
    public class Input
    {
        private readonly Window _window;
        private KeyboardState _currentKeyboardState;

        private KeyboardState _prevKeyboardState;
        private MouseState _currentMouseState;
        private MouseState _prevMouseState;

        private bool _firstMove = true;
        private Vector2 _mousePos;
        private Vector2 _deltaMouse;

        public Input(Window window)
        {
            _window = window;
        }
        
        public void OnUpdateFrame()
        {
            _prevKeyboardState = _currentKeyboardState;
            _currentKeyboardState = _window.KeyboardState.GetSnapshot();
            
            _prevMouseState = _currentMouseState;
            _currentMouseState = _window.MouseState.GetSnapshot();
            
            if (_firstMove)
            {
                _mousePos = new Vector2(_currentMouseState.X, _currentMouseState.Y);
                _firstMove = false;
            }
            else
            {
                var deltaX = _currentMouseState.X - _mousePos.X;
                var deltaY = _currentMouseState.Y - _mousePos.Y;

                _deltaMouse = new Vector2(deltaX, deltaY);
                _mousePos = new Vector2(_currentMouseState.X, _currentMouseState.Y);
            }

            // Exit
            if (KeyDown(Keys.Escape))
            {
                _window.Close();
            }
            if (KeyDown(Keys.Space))
            {
                Engine.Instance.RecompileShader();
            }
        }

        ///<summary>return vector containing mouse shift on x and y
        public Vector2 MouseDeltaPos()
        {
            return _deltaMouse;
        }
        public Vector2 MousePosition()
        {
            return _mousePos;
        }
        
        ///<summary>user is holding key down</summary>
        public bool KeyDown(Keys key)
        {
            return _window.KeyboardState.IsKeyDown(key);
        }

        ///<summary>specified key is not pressed</summary>
        public bool KeyUp(Keys key)
        {
            return _currentKeyboardState.IsKeyReleased(key);
        }

        ///<summary>user just smashed the key</summary>
        public bool GetKeyDown(Keys key)
        {
            return _currentKeyboardState.IsKeyDown(key) && _prevKeyboardState.IsKeyReleased(key);
        }

        ///<summary>user just released the key</summary>
        public bool GetKeyUp(Keys key)
        {
            return _currentKeyboardState.IsKeyReleased(key) && _prevKeyboardState.IsKeyDown(key);
        }

        public bool KeyDown(MouseButton key)
        {
            return _currentMouseState[key];
        }

        public bool KeyUp(MouseButton key)
        {
            return !_currentMouseState[key];
        }

        public bool GetKeyDown(MouseButton key)
        {
            return !_prevMouseState[key] && _currentMouseState[key];
        }

        ///<summary>user just released the key</summary>
        public bool GetKeyUp(MouseButton key)
        {
            return _prevMouseState[key] && !_currentMouseState[key];
        }

        ///<summary>how much user scrolled mouse wheel</summary>
        public float MouseWheel()
        {
            return MouseDeltaPos().Y;
        }
    }