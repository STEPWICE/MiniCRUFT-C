namespace MiniCRUFT.Core;

public sealed class FirstPersonConfig
{
    public bool Enabled { get; set; } = true;
    public float HandForwardOffset { get; set; } = 0.62f;
    public float HandRightOffset { get; set; } = 0.38f;
    public float HandDownOffset { get; set; } = 0.26f;
    public float HandWidth { get; set; } = 0.17f;
    public float HandHeight { get; set; } = 0.58f;
    public float HandDepth { get; set; } = 0.17f;
    public float HandMotionScale { get; set; } = 1.0f;
    public float HandSwingScale { get; set; } = 1.0f;
    public float ItemForwardOffset { get; set; } = 1.16f;
    public float ItemRightOffset { get; set; } = 0.30f;
    public float ItemDownOffset { get; set; } = 0.24f;
    public float ItemScale { get; set; } = 0.20f;
    public float TransparentScale { get; set; } = 0.16f;
    public float ItemMotionScale { get; set; } = 1.0f;
    public float ItemSwingScale { get; set; } = 1.0f;
    public float SwingCurvePower { get; set; } = 2.0f;
    public float ItemPitch { get; set; } = -14f;
    public float ItemYaw { get; set; } = -36f;
    public float ItemRoll { get; set; } = -6f;
    public float SwingDurationSeconds { get; set; } = 0.38f;
    public float SwingTranslationX { get; set; } = -0.12f;
    public float SwingTranslationY { get; set; } = 0.06f;
    public float SwingTranslationZ { get; set; } = -0.12f;
    public float SwingRotationX { get; set; } = -50f;
    public float SwingRotationY { get; set; } = 18f;
    public float SwingRotationZ { get; set; } = 56f;
    public float MovementBobAmplitude { get; set; } = 0.016f;
    public float MovementBobSpeed { get; set; } = 5.5f;
    public float IdleBobAmplitude { get; set; } = 0.008f;
    public float IdleBobSpeed { get; set; } = 1.8f;
    public float MotionRotationDegrees { get; set; } = 4f;
    public float CrossScale { get; set; } = 0.24f;
    public float TorchScale { get; set; } = 0.26f;
    public float CardThickness { get; set; } = 0.016f;
}
