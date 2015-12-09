using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EntityNetwork;

namespace Basic
{
    public interface IBullet : IEntityPrototype
    {
        void Hit(float x = 0f, float y = 0f);
    }
}
