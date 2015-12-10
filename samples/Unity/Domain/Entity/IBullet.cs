using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EntityNetwork;

namespace Domain.Entity
{
    public interface IBullet : IEntityPrototype
    {
        void Hit(float x = 0f, float y = 0f);
    }
}
