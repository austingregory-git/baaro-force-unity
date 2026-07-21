using System.Collections.Generic;

namespace BaaroForce.Animations
{
    /// <summary>
    /// Import settings required for each directional sprite referenced here (Texture Type: Sprite):
    /// Sprite Mode = Single, Pivot = Custom, positioned at the character's feet (not center) so the
    /// sprite sits on top of its tile instead of sinking into it — see MapTile.PlaceUnit.
    /// </summary>
    public class SpriteKit
    {
        public string BackLeftSpritePath { get; private set; }
        public string BackRightSpritePath { get; private set; }
        public string FrontLeftSpritePath { get; private set; }
        public string FrontRightSpritePath { get; private set; }
        public List<string> IdleSpritePaths { get; private set; }
        public List<string> WalkSpritePaths { get; private set; }
        public List<string> AttackSpritePaths { get; private set; }
        public List<string> DeathSpritePaths { get; private set; }

        public SpriteKit(
            string backLeftSpritePath, 
            string backRightSpritePath, 
            string frontLeftSpritePath, 
            string frontRightSpritePath, 
            List<string> idleSpritePaths, 
            List<string> walkSpritePaths, 
            List<string> attackSpritePaths, 
            List<string> deathSpritePaths)
        {
            BackLeftSpritePath = backLeftSpritePath;
            BackRightSpritePath = backRightSpritePath;
            FrontLeftSpritePath = frontLeftSpritePath;
            FrontRightSpritePath = frontRightSpritePath;
            IdleSpritePaths = idleSpritePaths;
            WalkSpritePaths = walkSpritePaths;
            AttackSpritePaths = attackSpritePaths;
            DeathSpritePaths = deathSpritePaths;
        }
    }
}