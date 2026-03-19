using System;
using MiniCRUFT.Core;
using MiniCRUFT.World;

namespace MiniCRUFT.Game;

public sealed class BlockBreakSystem
{
    private BlockCoord _target;
    private BlockId _block;
    private BlockId _tool;
    private BlockId _drop;
    private int _count;
    private float _durationSeconds;
    private float _elapsedSeconds;
    private bool _toolUsed;
    private bool _active;

    public bool IsActive => _active;
    public BlockCoord Target => _target;
    public BlockId Block => _block;
    public BlockId Tool => _tool;
    public BlockId Drop => _drop;
    public int Count => _count;
    public bool ToolUsed => _toolUsed;
    public float DurationSeconds => _durationSeconds;

    public float Progress
    {
        get
        {
            if (!_active || _durationSeconds <= 0f)
            {
                return 0f;
            }

            return Math.Clamp(_elapsedSeconds / _durationSeconds, 0f, 1f);
        }
    }

    public void Cancel()
    {
        _active = false;
        _elapsedSeconds = 0f;
        _durationSeconds = 0f;
        _count = 0;
        _toolUsed = false;
        _drop = BlockId.Air;
        _block = BlockId.Air;
        _tool = BlockId.Air;
        _target = default;
    }

    public bool TryStart(BlockCoord target, BlockId block, BlockId tool, BlockId drop, int count, bool toolUsed, float durationSeconds)
    {
        if (durationSeconds <= 0f || float.IsNaN(durationSeconds) || float.IsInfinity(durationSeconds))
        {
            return false;
        }

        if (block == BlockId.Air || count < 0)
        {
            return false;
        }

        _target = target;
        _block = block;
        _tool = tool;
        _drop = drop;
        _count = count;
        _toolUsed = toolUsed;
        _durationSeconds = durationSeconds;
        _elapsedSeconds = 0f;
        _active = true;
        return true;
    }

    public bool Update(float dt)
    {
        if (!_active || dt <= 0f)
        {
            return false;
        }

        _elapsedSeconds += dt;
        if (_elapsedSeconds < _durationSeconds)
        {
            return false;
        }

        _active = false;
        return true;
    }
}
