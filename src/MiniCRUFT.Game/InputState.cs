using Veldrid;

namespace MiniCRUFT.Game;

public sealed class InputState
{
    public bool Forward { get; set; }
    public bool Backward { get; set; }
    public bool Left { get; set; }
    public bool Right { get; set; }
    public bool Jump { get; set; }
    public bool Sprint { get; set; }

    public bool MouseLeft { get; set; }
    public bool MouseRight { get; set; }

    public float MouseDeltaX { get; set; }
    public float MouseDeltaY { get; set; }
    public int MouseWheelDelta { get; set; }

    public void ResetDeltas()
    {
        MouseDeltaX = 0;
        MouseDeltaY = 0;
        MouseWheelDelta = 0;
    }
}
