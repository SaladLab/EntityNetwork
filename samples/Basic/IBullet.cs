using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EntityNetwork;
using TypeAlias;

namespace Basic
{
    [TypeAlias]
    public interface IBullet : IEntityPrototype
    {
        void Hit(float x = 0f, float y = 0f);
    }
}
