using System.Numerics;
using MiniCRUFT.Core;
using MiniCRUFT.World;

namespace MiniCRUFT.Renderer;

public interface IParticleEmitter
{
    void EmitBlockBreakParticles(BlockId block, Vector3 position, Vector3 motion);
    void EmitBlockPlaceParticles(BlockId block, Vector3 position, Vector3 motion);
    void EmitStepParticles(BlockId block, Vector3 position, Vector3 motion, bool sprinting);
    void EmitJumpParticles(BlockId block, Vector3 position, Vector3 motion);
    void EmitMobAttackParticles(MobType type, Vector3 position, Vector3 motion, bool elite = false, EliteMobVariant eliteVariant = EliteMobVariant.None, float intensity = 1f);
    void EmitMobHurtParticles(MobType type, Vector3 position, Vector3 motion, bool elite = false, EliteMobVariant eliteVariant = EliteMobVariant.None, float intensity = 1f);
    void EmitMobDeathParticles(MobType type, Vector3 position, Vector3 motion, bool elite = false, EliteMobVariant eliteVariant = EliteMobVariant.None, float intensity = 1f);
    void EmitExplosionParticles(Vector3 position, int affectedBlocks, float intensity = 1f);
    void EmitFireParticles(FireEventKind kind, Vector3 position, float intensity = 1f);
}
