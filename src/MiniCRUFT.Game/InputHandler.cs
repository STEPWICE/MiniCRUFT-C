using Veldrid;
using Veldrid.Sdl2;

namespace MiniCRUFT.Game;

public sealed class InputHandler
{
    private readonly InputState _state;
    private bool _hasMousePosition;
    private float _lastMouseX;
    private float _lastMouseY;
    private bool _useRelative;

    public InputHandler(InputState state)
    {
        _state = state;
    }

    public void Attach(Sdl2Window window)
    {
        window.KeyDown += OnKeyDown;
        window.KeyUp += OnKeyUp;
        window.MouseMove += OnMouseMove;
        window.MouseDown += OnMouseDown;
        window.MouseUp += OnMouseUp;
        window.MouseWheel += OnMouseWheel;
    }

    public void SetRelative(bool enabled)
    {
        _useRelative = enabled;
        ResetMouse();
    }

    public void UpdateMouseDelta(Sdl2Window window)
    {
        if (!_useRelative)
        {
            return;
        }

        var delta = window.MouseDelta;
        if (delta.X == 0f && delta.Y == 0f)
        {
            return;
        }

        _state.MouseDeltaX += delta.X;
        _state.MouseDeltaY += delta.Y;
    }

    public void ResetMouse()
    {
        _hasMousePosition = false;
        _lastMouseX = 0f;
        _lastMouseY = 0f;
    }

    private void OnKeyDown(KeyEvent key)
    {
        switch (key.Key)
        {
            case Key.W: _state.Forward = true; break;
            case Key.S: _state.Backward = true; break;
            case Key.A: _state.Left = true; break;
            case Key.D: _state.Right = true; break;
            case Key.Space: _state.Jump = true; break;
            case Key.ShiftLeft: _state.Sprint = true; break;
        }
    }

    private void OnKeyUp(KeyEvent key)
    {
        switch (key.Key)
        {
            case Key.W: _state.Forward = false; break;
            case Key.S: _state.Backward = false; break;
            case Key.A: _state.Left = false; break;
            case Key.D: _state.Right = false; break;
            case Key.Space: _state.Jump = false; break;
            case Key.ShiftLeft: _state.Sprint = false; break;
        }
    }

    private void OnMouseMove(MouseMoveEventArgs args)
    {
        if (_useRelative)
        {
            return;
        }

        float x = args.MousePosition.X;
        float y = args.MousePosition.Y;
        if (_hasMousePosition)
        {
            _state.MouseDeltaX += x - _lastMouseX;
            _state.MouseDeltaY += y - _lastMouseY;
        }
        _lastMouseX = x;
        _lastMouseY = y;
        _hasMousePosition = true;
    }

    private void OnMouseDown(MouseEvent args)
    {
        if (args.MouseButton == MouseButton.Left)
        {
            _state.MouseLeft = true;
        }
        else if (args.MouseButton == MouseButton.Right)
        {
            _state.MouseRight = true;
        }
    }

    private void OnMouseUp(MouseEvent args)
    {
        if (args.MouseButton == MouseButton.Left)
        {
            _state.MouseLeft = false;
        }
        else if (args.MouseButton == MouseButton.Right)
        {
            _state.MouseRight = false;
        }
    }

    private void OnMouseWheel(MouseWheelEventArgs args)
    {
        _state.MouseWheelDelta += (int)args.WheelDelta;
    }
}
