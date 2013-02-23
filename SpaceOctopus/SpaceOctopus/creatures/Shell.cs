using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Audio;
using System.Diagnostics;

namespace SpaceOctopus.creatures
{
    public class Shell : Enemy
    {
        public Shell()
            : base(Core.Instance.Art.SnailShell, 0, 0)
        {
        }

        public override void Shoot()
        {
        }

        public override void Move(int delta)
        {
            if (!IsAlive) return;
            DoCollisions();
        }

    }
}
